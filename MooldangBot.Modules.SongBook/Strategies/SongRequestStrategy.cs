using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Domain.Common;
using MediatR;
using MooldangBot.Modules.SongBookModule.Features.Commands.RequestSong;
using MooldangBot.Contracts.Commands.Interfaces;

namespace MooldangBot.Modules.SongBookModule.Strategies;

/// <summary>
/// [오르페우스의 조율]: 곡 신청(Song) 명령어를 처리하는 전략입니다. (Thin Orchestrator)
/// </summary>
public class SongRequestStrategy(
    IMediator mediator,
    IChzzkBotService botService,
    ILogger<SongRequestStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "SongRequest";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent_Legacy notification, UnifiedCommand command, CancellationToken ct)
    {
        string msg = notification.Message.Trim();
        string[] parts = msg.Split(' ', 2);
        if (parts.Length < 2)
        {
            await botService.SendReplyChatAsync(notification.Profile, "신청곡 제목을 함께 입력해 주세요! (예: !신청 제목) 🎵", notification.SenderId, ct);
            return CommandExecutionResult.Failure("신청곡 제목 누락", shouldRefund: true);
        }

        string songTitle = parts[1];
        
        try
        {
            // 🚀 [수직 분할 집도]: 비즈니스 로직을 직접 처리하지 않고 MediatR를 통해 위임합니다.
            return await mediator.Send(new RequestSongCommand(notification, command, songTitle), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"❌ [SongRequestStrategy] 위임 오류: {ex.Message}");
            await botService.SendReplyChatAsync(notification.Profile, "⚠️ 곡 신청 처리 중 서버 오류가 발생했습니다.", notification.SenderId, ct);
            return CommandExecutionResult.Failure("곡 신청 서버 오류", shouldRefund: true);
        }
    }
}

