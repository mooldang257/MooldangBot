using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Contracts.Chzzk.Interfaces;
using MooldangBot.Modules.SongBookModule.Abstractions;
using MooldangBot.Domain.Events;

namespace MooldangBot.Modules.SongBookModule.Features.Commands;

/// <summary>
/// [시뮬레이션 기동]: 채팅 시뮬레이션 이벤트를 발생시켜 시스템 동작을 테스트합니다.
/// </summary>
public record SimulatorChatCommand(string ChzzkUid, string Message, int Donation = 0) : IRequest<Result<object>>;

public class SimulatorChatHandler(
    ISongBookDbContext db, 
    IMediator mediator, 
    IChzzkApiClient chzzkApi) : IRequestHandler<SimulatorChatCommand, Result<object>>
{
    public async Task<Result<object>> Handle(SimulatorChatCommand request, CancellationToken ct)
    {
        var profile = await db.StreamerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == request.ChzzkUid.ToLower() && !p.IsDeleted, ct);
            
        if (profile == null) 
            return Result<object>.Failure("스트리머를 찾을 수 없습니다.");

        // [v15.1]: 기존 레거시 이벤트 발행 (호환성 유지)
        await mediator.Publish(new ChatMessageReceivedEvent_Legacy(
            Guid.NewGuid(),
            profile, 
            "시뮬레이터", 
            request.Message, 
            "streamer", 
            "simulator_sender_id", 
            null, 
            request.Donation
        ), ct);

        if (!string.IsNullOrEmpty(request.Message) && !string.IsNullOrEmpty(profile.ChzzkAccessToken))
        {
            // 실제 치지직 채팅 전송 (연동 테스트용)
            await chzzkApi.SendChatMessageAsync(profile.ChzzkAccessToken, profile.ChzzkUid, request.Message);
        }

        return Result<object>.Success(new { message = "시뮬레이션 채팅이 전송되었습니다." });
    }
}
