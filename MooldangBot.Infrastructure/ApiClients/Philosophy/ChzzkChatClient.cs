using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Events;
using Polly;
using Polly.Retry;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Entities.Philosophy;

namespace MooldangBot.Infrastructure.ApiClients.Philosophy;

/// <summary>
/// [피닉스의 심장]: 실제 치지직 WebSocket 연결 및 데이터 수신을 관리하는 실전 구현체입니다.
/// </summary>
public class ChzzkChatClient : IChzzkChatClient, IDisposable
{
    private readonly ILogger<ChzzkChatClient> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, ClientWebSocket> _clients = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsList = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastActivityList = new(); // [v2.1.8] 마지막 수신 시간 추적
    private readonly AsyncRetryPolicy _retryPolicy;

    public ChzzkChatClient(ILogger<ChzzkChatClient> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                (ex, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"[피닉스 재시도] {retryCount}회차 시도. 사유: {ex.Message}");
                });
    }

    public bool IsConnected(string chzzkUid)
    {
        if (!_clients.TryGetValue(chzzkUid, out var ws) || ws.State != WebSocketState.Open)
            return false;

        // [v2.1.8] 좀비 세션 체크: 1분간 아무런 메시지(2/3 포함)가 없으면 죽은 것으로 간주
        if (_lastActivityList.TryGetValue(chzzkUid, out var lastActivity))
        {
            if (DateTime.UtcNow - lastActivity > TimeSpan.FromMinutes(1))
            {
                _logger.LogWarning($"👻 [좀비 감지] {chzzkUid} 채널이 1분간 침묵 중입니다. 강제 재건을 트리거합니다.");
                return false; 
            }
        }
        else
        {
            // 아직 한 번도 데이터가 안 왔다면, 연결 직후이므로 잠시 기다려줌
            _lastActivityList[chzzkUid] = DateTime.UtcNow;
        }

        return true;
    }

    public async Task<bool> ConnectAsync(string chzzkUid, string accessToken)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await DisconnectAsync(chzzkUid);

            var ws = new ClientWebSocket();
            var cts = new CancellationTokenSource();
            
            ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0");
            ws.Options.SetRequestHeader("Origin", "https://chzzk.naver.com");

            // [서기의 기록]: 실제 치지직 채팅 서버 URL 조립 로직
            using var scope = _scopeFactory.CreateScope();
            var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
            
            var sessionAuth = await chzzkApi.GetSessionAuthAsync(accessToken);
            if (sessionAuth == null || string.IsNullOrEmpty(sessionAuth.Content?.Url))
            {
                throw new Exception("치지직 채팅 세션 인증 정보를 가져오지 못했습니다.");
            }

            string socketUrl = sessionAuth.Content.Url;
            // Socket.IO 필터 대응 (Chzzk 사양)
            if (!socketUrl.Contains("transport=websocket"))
            {
                var uriBuilder = new UriBuilder(socketUrl) { Scheme = "wss" };
                if (uriBuilder.Path == "/") uriBuilder.Path = "/socket.io/";
                string extraQuery = "transport=websocket&EIO=3";
                uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) ? extraQuery : uriBuilder.Query.Substring(1) + "&" + extraQuery;
                socketUrl = uriBuilder.ToString();
            }

            await ws.ConnectAsync(new Uri(socketUrl), cts.Token);

            _clients[chzzkUid] = ws;
            _ctsList[chzzkUid] = cts;

            // [파동의 이중 결속]: 수신 루프와 핑 루프를 한 몸으로 묶어 실행 (v2.2.0 운영본 로직 이식)
            _ = Task.Run(async () => 
            {
                var receiveTask = ReceiveLoopAsync(chzzkUid, ws, cts.Token);
                var pingTask = PingLoopAsync(chzzkUid, ws, cts.Token);

                var completedTask = await Task.WhenAny(receiveTask, pingTask);
                
                if (completedTask.IsFaulted)
                {
                    _logger.LogError(completedTask.Exception, $"❌ [피닉스의 결속] {chzzkUid} 루프 내 심각한 오류 발생!");
                }
                
                _logger.LogWarning($"⚠️ [피닉스의 결속] {chzzkUid} 세션 루프 중 하나가 종료되었습니다. 재건을 위해 전체 정화를 시작합니다.");
                await DisconnectAsync(chzzkUid);
            }, cts.Token);

            return true;
        });
    }

    // 🩺 [심장 박동]: 치지직 서버에 10초마다 생존 신고 (v2.2.0 운영본 로직 이식)
    private async Task PingLoopAsync(string chzzkUid, ClientWebSocket ws, CancellationToken ct)
    {
        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                if (ws.State == WebSocketState.Open)
                {
                    await SendRawAsync(ws, "2", ct);
                    _logger.LogDebug($"📡 [심장박동] {chzzkUid} Active Ping 완료");
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ [심장박동] {chzzkUid} 전송 중 이상 감지: {ex.Message}");
                throw; // Task.WhenAny에서 감지하도록 던짐
            }
        }
    }

    private async Task ReceiveLoopAsync(string chzzkUid, ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[1024 * 16];
        _logger.LogInformation($"[파동의 경청] {chzzkUid} 채널의 수신 루프가 활성화되었습니다.");

        try
        {
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var message = Encoding.UTF8.GetString(ms.ToArray());
                _lastActivityList[chzzkUid] = DateTime.UtcNow; // [v2.1.8] 활동성 기록 (침묵의 시간 방지)

                if (string.IsNullOrEmpty(message)) continue;

        // [침묵 속의 울림]: 수신된 패킷 분석
        _logger.LogDebug($"[로우 패킷] {chzzkUid}: {message}");
        await HandleSocketPacketAsync(chzzkUid, ws, message, ct);
            }
        }
        catch (OperationCanceledException) { /* 정상 종료 */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[파동의 경청 에러] {chzzkUid} 수신 루프 중단");
        }
        finally
        {
            _logger.LogWarning($"[침묵 속의 울림] {chzzkUid} 채널이 수신 루프에서 벗어났습니다. 정화를 준비합니다.");
            await DisconnectAsync(chzzkUid);
        }
    }

    private async Task HandleSocketPacketAsync(string chzzkUid, ClientWebSocket ws, string message, CancellationToken ct)
    {
        // 1. [생존의 확인]: Ping(2)에 대해 Pong(3)으로 응답
        if (message == "2")
        {
            await SendRawAsync(ws, "3", ct);
            return;
        }

        // 2. [입장 수락 확인]: 커넥션 직후 0 패킷을 받으면 40(Admission) 패킷 전송
        if (message.StartsWith("0"))
        {
            await SendRawAsync(ws, "40", ct);
            return;
        }

        // 2. [유기적 전달]: 채팅 이벤트(42로 시작하는 JSON) 파싱 및 발행
        if (message.StartsWith("42"))
        {
            string json = message.Substring(2);
            
            // 🚀 [v2.0.0] Task.Run을 통해 백그라운드 태스크로 이벤트를 위임하여 소켓 루프 블로킹 차단
            _ = Task.Run(async () => 
            {
                try 
                {
                    // 루프의 정지 토큰(ct) 대신 새로운 생명주기 제어가 필요할 수 있으나, 
                    // 여기서는 기본적으로 비동기 처리를 독립적으로 수행합니다.
                    await DispatchEventAsync(chzzkUid, json, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ [피닉스의 심장] 백그라운드 이벤트 처리 중 오류 발생 (ID: {chzzkUid})");
                }
            }, ct);
        }
    }

    private async Task DispatchEventAsync(string chzzkUid, string json, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediatr = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            
            // 패킷 구조 해석 (치지직 사양 기반)
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string eventName = root[0].GetString() ?? "";

            if (eventName == "SYSTEM")
            {
                var payloadString = root[1].GetString() ?? "{}";
                using var payloadDoc = JsonDocument.Parse(payloadString);
                var payload = payloadDoc.RootElement;
                
                if (payload.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "connected")
                {
                    string sessionKey = payload.GetProperty("data").GetProperty("sessionKey").GetString() ?? "";
                    
                    // [파동의 공유]: 이벤트 구독 (Chat, Donation)
                    var chzzkApi = scope.ServiceProvider.GetRequiredService<IChzzkApiClient>();
                    
                    // 액세스 토큰은 스트리머 프로필에서 가져와야 함 (이벤트 핸들러 아키텍처 상의 개선 포인트)
                    // 여기서는 현재 연결에 사용된 토큰을 보존하거나 다시 조회해야 합니다. (우선 DB 재조회)
                    var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                    if (profile != null)
                    {
                        bool chatSub = await chzzkApi.SubscribeEventAsync(profile.ChzzkAccessToken!, sessionKey, "chat", chzzkUid);
                        bool donationSub = await chzzkApi.SubscribeEventAsync(profile.ChzzkAccessToken!, sessionKey, "donation", chzzkUid);
                        
                        if (chatSub && donationSub)
                            _logger.LogInformation($"✨ [유기적 구독] {chzzkUid} 채널의 이벤트 구독이 완료되었습니다.");
                        else
                            _logger.LogWarning($"⚠️ [구독 실패] {chzzkUid} 채널 구독 중 일부 실패 (Chat: {chatSub}, Donation: {donationSub})");
                    }
                }
            }
            else if (eventName == "CHAT")
            {
                // [오시리스의 저울]: 채팅 메시지 처리
                var payloadString = root[1].GetString() ?? "{}";
                using var payloadDoc = JsonDocument.Parse(payloadString);
                var payload = payloadDoc.RootElement;
                
                string msg = payload.GetProperty("content").GetString() ?? "";
                string senderId = payload.TryGetProperty("senderChannelId", out var idProp) ? idProp.GetString() ?? "" : "";

                // 프로필 정보 조회 (채팅 로그용)
                var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                if (profile != null)
                {
                    // 💡 [텔로스5의 분신]: 중복 처리 방지를 위해 여기서 직접 이벤트를 발행하거나 핸들러를 호출합니다.
                    // 기존 ChatMessageReceivedEvent 아키텍처에 맞춤
                    var profileJson = payload.GetProperty("profile").ValueKind == JsonValueKind.String
                                            ? payload.GetProperty("profile").GetString() ?? "{}"
                                            : payload.GetProperty("profile").GetRawText();

                    using var profileDoc = JsonDocument.Parse(profileJson);
                    string nickname = profileDoc.RootElement.TryGetProperty("nickname", out var n) ? n.GetString() ?? "시청자" : "시청자";
                    string userRole = profileDoc.RootElement.TryGetProperty("userRoleCode", out var r) ? r.GetString() ?? "common_user" : "common_user";

                    await mediatr.Publish(new ChatMessageReceivedEvent(profile, nickname, msg, userRole, senderId, null, 0), ct);
                }
            }
            else if (eventName == "DONATION")
            {
                // [오시리스의 상점]: 후원 처리
                var payloadString = root[1].GetString() ?? "{}";
                using var payloadDoc = JsonDocument.Parse(payloadString);
                var payload = payloadDoc.RootElement;

                int cheeseAmount = payload.TryGetProperty("payAmount", out var p) ? p.GetInt32() : 0;
                string msg = payload.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                string senderId = payload.TryGetProperty("senderChannelId", out var sid) ? sid.GetString() ?? "" : "";

                _logger.LogInformation($"💰 [후원 감지] {chzzkUid} 채널에서 {cheeseAmount}치즈 후원 발생: {msg}");

                var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                if (profile != null)
                {
                    var profileJson = payload.TryGetProperty("profile", out var prof) 
                                        ? (prof.ValueKind == JsonValueKind.String ? prof.GetString() ?? "{}" : prof.GetRawText())
                                        : "{}";

                    using var profileDoc = JsonDocument.Parse(profileJson);
                    string nickname = profileDoc.RootElement.TryGetProperty("nickname", out var n) ? n.GetString() ?? "후원자" : "후원자";
                    string userRole = "donation_user"; 

                    await mediatr.Publish(new ChatMessageReceivedEvent(profile, nickname, msg, userRole, senderId, null, cheeseAmount), ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"[전달 실패] 패킷 해석 불가: {ex.Message}");
        }
    }

    private async Task SendRawAsync(ClientWebSocket ws, string content, CancellationToken ct)
    {
        if (ws.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(content);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    public async Task DisconnectAsync(string chzzkUid)
    {
        if (_ctsList.TryRemove(chzzkUid, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        _lastActivityList.TryRemove(chzzkUid, out _); // [v2.1.8] 마지막 수신 기록 제거

        if (_clients.TryRemove(chzzkUid, out var ws))
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Rebuilding", CancellationToken.None);
            }
            catch { }
            finally { ws.Dispose(); }
        }
    }

    public int GetActiveConnectionCount() => _clients.Count;

    public void Dispose()
    {
        foreach (var uid in _clients.Keys) DisconnectAsync(uid).Wait();
    }
}
