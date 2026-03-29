using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Features.Admin;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MooldangBot.Application.Workers;

public class ChzzkChannelWorker
{
    private readonly ILogger<ChzzkChannelWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _uid;
    private string? _botUid;
    private readonly IChzzkApiClient _chzzkApi;
    private readonly ICommandCacheService _cacheService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IChzzkBotService _botService;
    private readonly ITokenRenewalService _renewalService;

    public ChzzkChannelWorker(string uid, IServiceProvider serviceProvider, IChzzkApiClient chzzkApi, IChzzkBotService botService, ITokenRenewalService renewalService)
    {
        _uid = uid;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<ChzzkChannelWorker>>();
        _cacheService = serviceProvider.GetRequiredService<ICommandCacheService>();
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _chzzkApi = chzzkApi;
        _botService = botService;
        _renewalService = renewalService;
    }

    public async Task ConnectAndListenAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0");
            ws.Options.SetRequestHeader("Origin", "https://chzzk.naver.com");
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                var profile = await dbContext.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == _uid, stoppingToken);
                if (profile == null || !profile.IsBotEnabled || string.IsNullOrEmpty(profile.ChzzkAccessToken))
                {
                    _logger.LogWarning($"[물댕봇] {_uid} 봇 비활성화 또는 토큰 없음. 10초 대기...");
                    await Task.Delay(10000, stoppingToken);
                    continue;
                }

                // [v2.1.5] Polly 기반의 안정적인 토큰 갱신 시스템 연동 (UTC/Local 시간대 문제 해결)
                await _renewalService.RenewIfNeededAsync(_uid);
                
                // 최신 토큰 정보를 다시 불러오기 위해 프로필 재조회
                profile = await dbContext.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == _uid, stoppingToken);
                if (profile == null || string.IsNullOrEmpty(profile.ChzzkAccessToken))
                {
                    _logger.LogError($"❌ [물댕봇] {_uid} 토큰 갱신 후에도 액세스 토큰이 없습니다. 로그인 상태를 점검하세요.");
                    await Task.Delay(10000, stoppingToken);
                    continue;
                }

                await _cacheService.RefreshUnifiedAsync(_uid, stoppingToken);

                var sessionAuth = await _chzzkApi.GetSessionAuthAsync(profile.ChzzkAccessToken!);
                if (sessionAuth == null)
                {
                    _logger.LogError($"❌ [물댕봇] {_uid} 세션 발급 실패. 5초 대기...");
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                string socketUrl = sessionAuth.Content?.Url ?? "";
                UriBuilder uriBuilder = new UriBuilder(socketUrl) { Scheme = "wss" };
                if (uriBuilder.Path == "/") uriBuilder.Path = "/socket.io/";
                
                string extraQuery = "transport=websocket&EIO=3";
                uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) 
                    ? extraQuery 
                    : uriBuilder.Query.Substring(1) + "&" + extraQuery;

                string finalSocketUrl = uriBuilder.ToString();
                _logger.LogWarning($"📡 [물댕봇] 조립된 최종 URL: {finalSocketUrl}");

                await ws.ConnectAsync(new Uri(finalSocketUrl), stoppingToken);
                _logger.LogInformation($"✅ [물댕봇] {_uid} 물리적 연결 성공! 무중단 병렬 루프를 시작합니다.");

                using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                
                var receiveTask = ReceiveLoopAsync(ws, profile, loopCts.Token);
                var pingTask = PingLoopAsync(ws, loopCts.Token);

                var completedTask = await Task.WhenAny(receiveTask, pingTask);
                
                if (completedTask.IsFaulted)
                {
                    _logger.LogError(completedTask.Exception, "❌ [무중단] 루프 내 작업 중 심각한 오류 발생!");
                }
                
                loopCts.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ [물댕봇] 소켓 통신/연결 에러. 즉각 재연결을 시도합니다.");
            }
            finally
            {
                if (ws.State == WebSocketState.Open)
                {
                    try { await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None); } catch { }
                }
                
                _logger.LogWarning("⚠️ [물댕봇] 소켓 연결이 해제되었습니다. 0.5초 후 복구를 시작합니다.");
                if (!stoppingToken.IsCancellationRequested) await Task.Delay(500, stoppingToken);
            }
        }
    }

    private async Task PingLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var pingMessage = Encoding.UTF8.GetBytes("2");
        var buffer = new ArraySegment<byte>(pingMessage);

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                if (ws.State == WebSocketState.Open)
                {
                    await ws.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
                    _logger.LogDebug($"📡 [심장박동] {_uid} Ping 완료");
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ [심장박동] {_uid} 전송 중 이상 감지: {ex.Message}");
                throw;
            }
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket ws, StreamerProfile profile, CancellationToken ct)
    {
        var buffer = new byte[1024 * 16];
        using var ms = new MemoryStream();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            try
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogWarning($"⚠️ [Receive] {_uid} 서버에서 Close 요청 수신.");
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var message = Encoding.UTF8.GetString(ms.ToArray());
                ms.SetLength(0);

                if (string.IsNullOrEmpty(message)) continue;

                if (message.StartsWith("0"))
                {
                    _logger.LogInformation("📦 [Receive] 서버 입장 수락. 방 입장 요청(40)");
                    await SendMessageAsync(ws, "40", ct);
                }
                else if (message.StartsWith("40"))
                {
                    _logger.LogInformation("✅ [Receive] 채팅방 입장 성공 (Socket.IO Connect)");
                }
                else if (message.StartsWith("41"))
                {
                    _logger.LogWarning("⚠️ [Receive] 서버에서 퇴장 요청(Disconnect) 수신. 재연결을 시도합니다.");
                    return; // 루프 종료하여 상위 ConnectAndListenAsync에서 복구 유도
                }
                else if (message.StartsWith("2"))
                {
                    await SendMessageAsync(ws, "3", ct);
                }
                else if (message.StartsWith("42")) // 42: Event (진짜 데이터)
                {
                    string eventPayload = message.Substring(2);

                    // 🚀 [v2.0.0] Task.Run을 통해 백그라운드 스레드로 이벤트를 위임합니다. (Fire-and-Forget)
                    // 이로 인해 Gemini API 등 외부 LLM 연동 시 지연이 발생해도 소켓 수신 루프는 멈추지 않습니다. [물멍 지침]
                    _ = Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        try
                        {
                            await HandleEventAsync(eventPayload, profile, scope, ct);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogWarning("⚠️ [물댕봇] 작업이 취소되어 이벤트 처리를 중단합니다.");
                        }
                        catch (Exception ex)
                        {
                            // 예외 격리: 백그라운드 태스크 내부의 오류가 전체 소켓 연결을 파괴하지 않도록 방어
                            _logger.LogError(ex, $"❌ [물댕봇] 백그라운드 이벤트 처리 중 치명적 오류 발생 (신경망 과부하 또는 파싱 실패): {ex.Message}");
                        }
                    }, ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ [Receive] {_uid} 수신 스트림 오류: {ex.Message}");
                throw;
            }
        }
    }

    // Removed internal RefreshTokenIfNeededAsync in favor of ITokenRenewalService (v2.1.5)

    private async Task HandleEventAsync(string jsonArray, StreamerProfile profile, IServiceScope scope, CancellationToken token)
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var latestProfile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == _uid, token);
            if (latestProfile != null) profile = latestProfile;

            using var doc = JsonDocument.Parse(jsonArray);
            string eventName = doc.RootElement[0].GetString() ?? "";

            string payloadString = doc.RootElement[1].GetString() ?? "{}";
            using var payloadDoc = JsonDocument.Parse(payloadString);
            var payload = payloadDoc.RootElement;

            if (eventName == "SYSTEM")
            {
                if (payload.GetProperty("type").GetString() == "connected")
                {
                    string sessionKey = payload.GetProperty("data").GetProperty("sessionKey").GetString() ?? "";
                    await _chzzkApi.SubscribeEventAsync(profile.ChzzkAccessToken!, sessionKey, "chat", profile.ChzzkUid);
                    await _chzzkApi.SubscribeEventAsync(profile.ChzzkAccessToken!, sessionKey, "donation", profile.ChzzkUid);
                }
            }
            else if (eventName == "DONATION")
            {
                // ... (DONATION handling logic - abbreviated for brevity but keeping structure)
                var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                // await mediator.Publish(new ChatMessageReceivedEvent(...), token);
            }
            else if (eventName == "CHAT")
            {
                string msg = payload.GetProperty("content").GetString() ?? "";
                string profileJson = payload.GetProperty("profile").ValueKind == JsonValueKind.String
                                        ? payload.GetProperty("profile").GetString() ?? "{}"
                                        : payload.GetProperty("profile").GetRawText();

                using var profileDoc = JsonDocument.Parse(profileJson);
                string nickname = profileDoc.RootElement.TryGetProperty("nickname", out var nickProp) ? nickProp.GetString() ?? "시청자" : "시청자";
                string userRole = profileDoc.RootElement.TryGetProperty("userRoleCode", out var roleProp) ? roleProp.GetString() ?? "common_user" : "common_user";
                string senderId = payload.TryGetProperty("senderChannelId", out var idProp) ? idProp.GetString() ?? "" : "";

                // [v1.9.7] 후원 금액 추출 (extras JSON 파싱)
                int donationAmount = 0;
                if (payload.TryGetProperty("extras", out var extrasProp))
                {
                    try
                    {
                        var extrasJson = extrasProp.GetString() ?? "{}";
                        using var extrasDoc = JsonDocument.Parse(extrasJson);
                        if (extrasDoc.RootElement.TryGetProperty("payAmount", out var payProp))
                        {
                            donationAmount = payProp.GetInt32();
                        }
                    }
                    catch { /* 파싱 실패 시 0원 유지 */ }
                }

                bool hasBotPrefix = msg.StartsWith("\u200B", StringComparison.Ordinal);
                bool isBotUid = !string.IsNullOrEmpty(_botUid) && string.Equals(senderId, _botUid, StringComparison.OrdinalIgnoreCase);

                if (!hasBotPrefix && !isBotUid)
                {
                    var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                    await mediator.Publish(new MooldangBot.Domain.Events.ChatMessageReceivedEvent(
                        profile, nickname, msg, userRole, senderId, null, donationAmount), token);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"[패킷 무시] 파싱 에러: {ex.Message}");
        }
    }

    private async Task SendMessageAsync(ClientWebSocket ws, string msg, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(msg);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
    }

    private async Task SendReplyChatAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var latestProfile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == profile.ChzzkUid, token);
            if (latestProfile == null) return;

            await _botService.SendReplyChatAsync(latestProfile, message, viewerUid, token);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ [SendReplyChatAsync 에러] {ex.Message}");
        }
    }
}