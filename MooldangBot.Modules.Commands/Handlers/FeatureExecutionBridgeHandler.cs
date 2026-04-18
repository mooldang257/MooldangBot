using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Modules.Commands.Events;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Modules.Commands.Models;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using MooldangBot.Modules.Commands.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Modules.Commands.Handlers;

/// <summary>
/// [신경교세포: 연결 브릿지]: CommandExecutedEvent를 수신하여 등록된 ICommandFeatureStrategy들을 실행합니다.
/// 기존 전략 패턴과 새로운 이벤트 기반 아키텍처 사이의 가교 역할을 수행합니다.
/// </summary>
public class FeatureExecutionBridgeHandler(
    IEnumerable<ICommandFeatureStrategy> strategies,
    ICommandDbContext db,
    IMediator mediator,
    ILogger<FeatureExecutionBridgeHandler> logger) : INotificationHandler<CommandExecutedEvent>
{
    public async Task Handle(CommandExecutedEvent notification, CancellationToken ct)
    {
        try
        {
            // [1. Filtering]: 실행 대상 중 브릿지가 필요한 기능형 명령어들 추출 (Reply 제외)
            var targets = notification.AllMatchedCommands
                .Where(c => c.FeatureType != CommandFeatureType.Reply)
                .ToList();

            if (!targets.Any()) return;

            // [2. Context Preparation]: 스트리머 프로필 획득 (전략 실행에 필요)
            var streamerProfile = await db.StreamerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ChzzkUid == notification.StreamerUid, ct);

            if (streamerProfile == null)
            {
                logger.LogWarning("⚠️ [BridgeHandler] 스트리머 프로필({StreamerUid})을 찾을 수 없어 전략을 실행하지 못했습니다.", notification.StreamerUid);
                return;
            }

            // [3. Legacy Event Reconstruction]: 기존 전략들이 기대하는 레거시 이벤트 객체 복구
            var legacyEvent = new ChatMessageReceivedEvent_Legacy(
                notification.CorrelationId,
                streamerProfile,
                notification.ViewerNickname,
                notification.RawMessage,
                "common_user", // 역할은 이벤트에 포함되지 않았으나 기본값으로 설정
                notification.ViewerUid,
                null, 
                notification.DonationAmount
            );

            // [4. Sequential Execution]: 정렬된 명령어 순서대로 전략 실행
            foreach (var commandMetadata in targets.OrderBy(t => t.Priority))
            {
                var strategy = strategies.FirstOrDefault(s => s.FeatureType == commandMetadata.FeatureType.ToString());
                if (strategy == null)
                {
                    logger.LogWarning("⚠️ [BridgeHandler] 기능 타입 '{FeatureType}'에 대응하는 전략을 찾을 수 없습니다.", commandMetadata.FeatureType);
                    continue;
                }

                // [물멍]: UnifiedCommand 엔티티를 더미나 캐시 데이터로 재구성 (전략 내부에서 ID 등이 필요할 수 있음)
                var commandEntity = new UnifiedCommand
                {
                    Id = commandMetadata.Id,
                    Keyword = commandMetadata.Keyword,
                    FeatureType = commandMetadata.FeatureType,
                    ResponseText = commandMetadata.ResponseText,
                    TargetId = commandMetadata.TargetId,
                    StreamerProfileId = streamerProfile.Id
                };

                logger.LogInformation("🚀 [BridgeHandler] 전략 실행 시작: {FeatureType} (Keyword: {Keyword})", strategy.FeatureType, commandEntity.Keyword);
                
                var result = await strategy.ExecuteAsync(legacyEvent, commandEntity, ct);

                if (result.IsSuccess)
                {
                    logger.LogInformation("✅ [BridgeHandler] 전략 실행 완료: {FeatureType}", strategy.FeatureType);
                    
                    // 📡 [오시리스의 확인]: 실행 완료 보고
                    await mediator.Publish(new FeatureExecutionCompletedEvent 
                    { 
                        CorrelationId = notification.CorrelationId,
                        FeatureType = strategy.FeatureType
                    }, ct);
                }
                else
                {
                    logger.LogWarning("❌ [BridgeHandler] 전략 실행 실패: {FeatureType} - {Error}", strategy.FeatureType, result.Message);
                    
                    // 📢 [오시리스의 조난 신호]: 실패 보고 (환불 트리거)
                    await mediator.Publish(new FeatureExecutionFailedEvent 
                    { 
                        CorrelationId = notification.CorrelationId,
                        FeatureType = strategy.FeatureType,
                        ErrorMessage = result.Message
                    }, ct);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [BridgeHandler] 브릿지 실행 중 치명적 오류 발생. (CorrelationId: {Id})", notification.CorrelationId);
        }
    }
}
