using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.Commands.SystemMessage;

/// <summary>
/// [하모니의 공지]: 치지직 방송 상단 공지를 처리하는 통합 전략입니다.
/// </summary>
public class NoticeStrategy(
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    ILogger<NoticeStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => CommandFeatureTypes.Notice;

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent_Legacy notification, UnifiedCommand command, CancellationToken ct)
    {
        // [v4.6.1] 명령어 뒤에 인수가 있으면 해당 내용을 공지로 사용, 없으면 기본 ResponseText 사용
        string rawMessage = notification.Message.Trim();
        string[] parts = rawMessage.Split(' ', 2);
        
        string responseTemplate = (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) 
            ? parts[1].Trim() 
            : command.ResponseText;

        return await ExecuteInternalAsync(notification, responseTemplate, ct);
    }

    private async Task<CommandExecutionResult> ExecuteInternalAsync(ChatMessageReceivedEvent_Legacy notification, string responseTemplate, CancellationToken ct)
    {
        try
        {
            string noticeMessage = await dynamicEngine.ProcessMessageAsync(
                responseTemplate,
                notification.Profile.ChzzkUid,
                notification.SenderId,
                notification.Username
            );
            
            // 치지직 플랫폼 상단 공지 등록 시도 (비동기 발행)
            await botService.SendReplyNoticeAsync(notification.Profile, noticeMessage, notification.SenderId, ct);
            
            return CommandExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[NoticeStrategy] 오류: {ex.Message}");
            return CommandExecutionResult.Failure("공지 처리 중 서버 오류가 발생했습니다.", shouldRefund: true);
        }
    }
}
