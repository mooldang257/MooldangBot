using MediatR;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using MooldangBot.Modules.SongBook.Features.Commands;

namespace MooldangBot.Modules.SongBook.Features.Strategies;

/// <summary>
/// [오시리스의 지휘]: 노래 신청 명령어(!신청 곡명)가 들어왔을 때 송북 모듈로 해당 요청을 전달합니다.
/// </summary>
public class SongRequestStrategy(IMediator mediator) : ICommandFeatureStrategy
{
    public string FeatureType => CommandFeatureTypes.SongRequest;

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent_Legacy notification, UnifiedCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(notification.Message)) 
            return CommandExecutionResult.Failure("곡명을 입력해주세요.");

        // [v15.1]: MediatR Command로 변환하여 내부 핸들러 호출
        // Arguments 추출 (명령어 키워드 제외)
        var arguments = notification.Message.Trim();
        if (arguments.StartsWith(command.Keyword))
        {
            arguments = arguments.Substring(command.Keyword.Length).Trim();
        }

        if (string.IsNullOrWhiteSpace(arguments))
            return CommandExecutionResult.Failure("신청할 곡 제목을 입력해주세요.");

        var result = await mediator.Send(new AddSongRequestCommand(notification.SenderId, notification.Username, arguments), ct);

        if (result.IsSuccess)
        {
            return CommandExecutionResult.Success($"'{arguments}' 곡이 신청되었습니다.");
        }
        else
        {
            return CommandExecutionResult.Failure(result.Error ?? "곡 신청에 실패했습니다.");
        }
    }
}
