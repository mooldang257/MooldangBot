using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Domain.Entities;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Modules.Commands.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Modules.Commands.Handlers;

/// <summary>
/// [신경 세포: 응답 기동]: CommandExecutedEvent를 수신하여 지능적으로 채팅 응답을 발송합니다.
/// 이제 허브의 직접 지시 없이도, 이벤트가 감지되면 자율적으로 입을 엽니다.
/// </summary>
public class ReplyCommandHandler(
    IChzzkBotService botService,
    ICommandDbContext db,
    IMediator mediator,
    ILogger<ReplyCommandHandler> logger) : INotificationHandler<CommandExecutedEvent>
{
    public async Task Handle(CommandExecutedEvent notification, CancellationToken ct)
    {
        try
        {
            // [1. Filtering]: 실행 대상 중 단순 응답(Reply)이면서 텍스트가 존재하는 항목 추출
            var targets = notification.AllMatchedCommands
                .Where(c => c.FeatureType == CommandFeatureType.Reply && !string.IsNullOrEmpty(c.ResponseText))
                .ToList();

            if (!targets.Any()) return;

            // [2. Context Preparation]: 스트리머 프로필 획득
            // [v4.0] 지휘관 지시: DB를 통해 검증된 프로필을 직접 획득하여 안정성을 확보합니다.
            var streamerProfile = await db.StreamerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ChzzkUid == notification.StreamerUid, ct);

            if (streamerProfile == null)
            {
                logger.LogWarning("⚠️ [ReplyHandler] 스트리머 프로필({StreamerUid})을 찾을 수 없어 응답을 전송하지 못했습니다.", notification.StreamerUid);
                return;
            }

            // [3. Message Composition]: 모든 응답 텍스트를 우선순위 순으로 결합
            var mergedResponse = string.Join("\n", targets
                .OrderBy(c => c.Priority)
                .Select(c => c.ResponseText));

            // [4. Execution]: 채팅 전송 (지휘관 지침: 답장 형식으로 발송)
            await botService.SendReplyChatAsync(streamerProfile, mergedResponse, notification.ViewerUid, ct);
            
            logger.LogInformation("✅ [ReplyHandler] {Count}개의 응답 발송 완료. (Target: {Viewer})", targets.Count, notification.ViewerNickname);

            // 📡 [오시리스의 확인]: Saga 사령부에 실행 완료 보고를 올립니다.
            await mediator.Publish(new FeatureExecutionCompletedEvent 
            { 
                CorrelationId = notification.CorrelationId,
                FeatureType = "Reply"
            }, ct);
        }
        catch (Exception ex)
        {
            // 🛡️ [격리 원칙]: 신경세포 내부의 장애가 함선 전체(허브)로 전이되지 않도록 방어막을 형성합니다.
            logger.LogError(ex, "❌ [ReplyHandler] 응답 기동 중 오류 발생. (CorrelationId: {Id})", notification.CorrelationId);

            // 📢 [오시리스의 조난 신호]: Saga 사령부에 실패 보고를 올려 자율 복구(환불)를 요청합니다.
            await mediator.Publish(new FeatureExecutionFailedEvent 
            { 
                CorrelationId = notification.CorrelationId,
                FeatureType = "Reply",
                ErrorMessage = ex.Message
            }, ct);
        }
    }
}
