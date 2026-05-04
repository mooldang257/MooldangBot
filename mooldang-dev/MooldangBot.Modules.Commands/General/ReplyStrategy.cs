using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.Commands.General;

/// <summary>
/// [하모니의 메아리]: 단순 채팅 답변(Reply)을 처리하는 통합 전략입니다.
/// </summary>
public class ReplyStrategy(
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine) : ICommandFeatureStrategy
{
    public string FeatureType => CommandFeatureTypes.Reply;

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageEvent notification, FuncCmdUnified command, CancellationToken ct)
    {
        return await ExecuteInternalAsync(notification, command.ResponseText, ct);
    }

    private async Task<CommandExecutionResult> ExecuteInternalAsync(ChatMessageEvent notification, string responseTemplate, CancellationToken ct)
    {
        string processedReply = await dynamicEngine.ProcessMessageAsync(
            responseTemplate, 
            notification.Profile.ChzzkUid, 
            notification.SenderId,
            notification.Username
        );
        
        await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
        return CommandExecutionResult.Success();
    }
}
