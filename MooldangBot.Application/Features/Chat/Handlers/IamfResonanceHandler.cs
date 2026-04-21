using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Features.Chat.Handlers;

/// <summary>
/// [공명 파이프라인]: 채팅 이벤트 발생 시 IAMF 철학에 따른 검증, 기록 및 조율을 수행합니다.
/// </summary>
public class IamfResonanceHandler(
    IRegulationService regulation,
    IPhoenixRecorder phoenix,
    IResonanceService resonance,
    IChatTrafficAnalyzer trafficAnalyzer,
    ILogger<IamfResonanceHandler> logger) : INotificationHandler<ChatMessageEvent>
{

    public async Task Handle(ChatMessageEvent notification, CancellationToken cancellationToken)
    {
        // 1. [오시리스의 규율] 파동 검증
        var (isValid, message) = await regulation.ValidateRegulationAsync(notification.Message, "Sephiroth");
        if (!isValid)
        {
            logger.LogWarning($"[IAMF 거부] {notification.Username}: {message}");
            return;
        }

        // 2. [피닉스의 기록] 시나리오 영속화
        await phoenix.RecordScenarioAsync(
            scenarioId: $"CHAT-{KstClock.Now:yyyyMMddHHmmss}",
            content: $"[{notification.Username}] {notification.Message}",
            level: 1
        );

        // 3. [방송의 혈류 감지]: 실시간 트래픽 분석 (Phase 4)
        var (systemLoad, interactionCount) = trafficAnalyzer.AnalyzeAndRecord(notification.Profile.ChzzkUid);

        // 4. [하모니의 조율] 실제 부하 기반 진동수 반영 [실전 감응]
        double dynamicHz = resonance.CalculateDynamicVibration(systemLoad, interactionCount);
        
        await resonance.AdjustResonanceAsync(notification.Profile.ChzzkUid, new MooldangBot.Domain.Contracts.AI.Models.Vibration(dynamicHz));

        logger.LogInformation($"[IAMF 공명 완료] {notification.Profile.ChzzkUid} - Load: {systemLoad:F2}, Count: {interactionCount}, Hz: {dynamicHz} Hz");
    }
}
