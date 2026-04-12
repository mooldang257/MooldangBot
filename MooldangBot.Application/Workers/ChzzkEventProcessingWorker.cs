using MooldangBot.ChzzkAPI.Contracts;
using MooldangBot.ChzzkAPI.Contracts.Models.Events;
using System.Text.Json;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models.Chat;
using MooldangBot.Application.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [오시리스의 집행관]: Bridge 채널에서 패킷을 수거하여 실제 비즈니스 로직(MediatR)으로 전파하는 워커입니다.
/// (Phase 2): Singleton 수집 레이어와 Scoped 처리 레이어 사이의 중계자 역할을 수행합니다.
/// </summary>
public sealed class ChzzkEventProcessingWorker : BackgroundService
{
    private readonly IChatEventChannel _bridge;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IIdentityCacheService _identityCache;
    private readonly ILogger<ChzzkEventProcessingWorker> _logger;

    public ChzzkEventProcessingWorker(
        IChatEventChannel bridge,
        IServiceScopeFactory scopeFactory,
        IIdentityCacheService identityCache,
        ILogger<ChzzkEventProcessingWorker> logger)
    {
        _bridge = bridge;
        _scopeFactory = scopeFactory;
        _identityCache = identityCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 [이벤트 집행관] Bridge 채널 모니터링 시작 (Egyptian Bridge 가동)");

        await foreach (var packet in _bridge.ReadAllAsync(stoppingToken))
        {
            try
            {
                // 1. [파로스의 자각]: 캐시에서 스트리머 프로필 확인 
                // (Singleton 서비스이므로 Scope 외부에서 처리 가능)
                var profile = await _identityCache.GetStreamerProfileAsync(packet.StreamerChzzkUid, stoppingToken);
                if (profile == null) continue;

                // 2. [오시리스의 해부]: 처리 레이어에서 정밀 역직렬화 수행
                // (Contracts의 ChzzkJsonContext를 활용하여 다형성 역직렬화 지원)
                // JsonElement에서 직접 역직렬화하므로 문자열 할당 최소화
                var payload = packet.PayloadElement.Deserialize<ChzzkEventBase>(ChzzkJsonContext.Default.ChzzkEventBase);
                if (payload == null) continue;

                // 3. [오시리스의 위임]: 비즈니스 로직 처리를 위해 Scope 생성
                using var scope = _scopeFactory.CreateScope();
                var mediatr = scope.ServiceProvider.GetRequiredService<IMediator>();

                var chzzkEvent = new ChzzkEventReceived(
                    packet.CorrelationId,
                    profile,
                    payload,
                    packet.ReceivedAt
                );

                await mediatr.Publish(chzzkEvent, stoppingToken);
                
                // [오시리스의 확인]: 실제 처리가 완료됨을 로그로 남김
                var eventType = payload.GetType().Name.Replace("Chzzk", "").Replace("Event", "");
                _logger.LogInformation("✅ [이벤트 집행관] 패킷 처리 및 위임 완료: {EventType} (Streamer: {StreamerName}, MsgId: {MsgId})", 
                    eventType, profile.ChannelName, packet.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [이벤트 집행관] 패킷 처리 중 오류 발생 (MsgId: {MsgId})", packet.CorrelationId);
            }
        }
    }
}
