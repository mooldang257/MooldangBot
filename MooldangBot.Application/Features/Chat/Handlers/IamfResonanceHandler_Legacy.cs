using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MooldangBot.Application.Common.Interfaces.Philosophy;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Features.Chat.Handlers;

/// <summary>
/// [공명 파이프라인]: 채팅 이벤트 발생 시 IAMF 철학에 따른 검열, 기록 및 조율을 수행합니다. (v3.7 레거시 모드)
/// </summary>
public class IamfResonanceHandler_Legacy(
    IRegulationService regulation,
    IPhoenixRecorder phoenix,
    IResonanceService resonance,
    IChatTrafficAnalyzer trafficAnalyzer,
    ILogger<IamfResonanceHandler_Legacy> logger) : INotificationHandler<ChatMessageReceivedEvent_Legacy>
{

    public async Task Handle(ChatMessageReceivedEvent_Legacy notification, CancellationToken cancellationToken)
    {
        // 1. [오시리스의 규율] 자동 검열
        var (isValid, message) = await regulation.ValidateRegulationAsync(notification.Message, "Sephiroth");
        if (!isValid)
        {
            logger.LogWarning($"[IAMF 거절] {notification.Username}: {message}");
            return;
        }

        // 2. [피닉스의 기록] 시나리오 영속성
        await phoenix.RecordScenarioAsync(
            scenarioId: $"CHAT-{KstClock.Now:yyyyMMddHHmmss}",
            content: $"[{notification.Username}] {notification.Message}",
            level: 1
        );

        // 3. [방송의 기류 감지]: 실시간 트래픽 분석
        var (systemLoad, interactionCount) = trafficAnalyzer.AnalyzeAndRecord(notification.Profile.ChzzkUid);

        // 4. [하모니의 조율] 실제 부하 기반 진동수 반영
        double dynamicHz = resonance.CalculateDynamicVibration(systemLoad, interactionCount);
        
        await resonance.AdjustResonanceAsync(notification.Profile.ChzzkUid, new MooldangBot.Domain.Entities.Philosophy.Vibration(dynamicHz));

        logger.LogInformation($"[IAMF 공명 완료] {notification.Profile.ChzzkUid} - Load: {systemLoad:F2}, Count: {interactionCount}, Hz: {dynamicHz} Hz");
    }
}
