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

    public bool IsConnected(string chzzkUid) => _clients.TryGetValue(chzzkUid, out var ws) && ws.State == WebSocketState.Open;

    public async Task<bool> ConnectAsync(string chzzkUid, string accessToken)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await DisconnectAsync(chzzkUid);

            var ws = new ClientWebSocket();
            var cts = new CancellationTokenSource();
            
            ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0");
            ws.Options.SetRequestHeader("Origin", "https://chzzk.naver.com");

            // [서기의 기록]: 실제 치지직 채팅 서버 URL 조립 로직 필요 (여기서는 예시 URL 사용)
            // 실제 실전 배포 시에는 IChzzkApiClient를 통해 동적으로 URL을 받아와야 함
            string socketUrl = $"wss://kr-ss1.chat.naver.com/chat"; 

            await ws.ConnectAsync(new Uri(socketUrl), cts.Token);

            _clients[chzzkUid] = ws;
            _ctsList[chzzkUid] = cts;

            // [파동의 경청]: 수신 루프를 백그라운드 태스크로 분리하여 실행
            _ = Task.Run(() => ReceiveLoopAsync(chzzkUid, ws, cts.Token), cts.Token);

            // 초기 입장 패킷 ("40" 등 치지직 사양에 맞는 요청 발송 필요)
            await SendRawAsync(ws, "40", cts.Token);

            return true;
        });
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
                if (string.IsNullOrEmpty(message)) continue;

                // [침묵 속의 울림]: 수신된 패킷 분석
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
            _logger.LogDebug($"[핑퐁] {chzzkUid} Pong 응답 전송");
            return;
        }

        // 2. [유기적 전달]: 채팅 이벤트(42로 시작하는 JSON) 파싱 및 발행
        if (message.StartsWith("42"))
        {
            var json = message.Substring(2);
            await DispatchEventAsync(chzzkUid, json, ct);
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

            if (eventName == "CHAT")
            {
                var payload = root[1];
                // ChatMessageReceivedEvent 발행 (구체적인 필드 매핑은 프로젝트 사양 준수)
                // await mediatr.Publish(new ChatMessageReceivedEvent(...), ct);
                _logger.LogInformation($"[유기적 전달] {chzzkUid} 채널에서 새로운 메시지 감지.");
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
