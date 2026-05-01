using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBook.Features.Queries;

/// <summary>
/// [오마카세 목록 요청]: 특정 스트리머의 활성화된 오마카세 리스트를 조회합니다.
/// </summary>
public record GetOmakaseListQuery(
    string ChzzkUid, 
    int? TargetId = null, 
    int? LastId = null, 
    int PageSize = 20) : IRequest<Result<object>>;

public class GetOmakaseListHandler(ISongBookDbContext db) : IRequestHandler<GetOmakaseListQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetOmakaseListQuery request, CancellationToken ct)
    {
        var profile = await db.CoreStreamerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == request.ChzzkUid.ToLower() && !p.IsDeleted, ct);
        
        if (profile == null)
            return Result<object>.Failure("스트리머를 찾을 수 없습니다.");

        var query = db.FuncStreamerOmakases.AsNoTracking()
            .Where(o => o.StreamerProfileId == profile.Id);

        if (request.TargetId.HasValue)
        {
            query = query.Where(o => o.Id == request.TargetId.Value);
        }

        if (request.LastId.HasValue && request.LastId.Value > 0)
        {
            query = query.Where(o => o.Id < request.LastId.Value);
        }

        // [v15.1]: 룰렛/오마카세 명령과의 Join 처리
        var items = await query
            .OrderByDescending(o => o.Id)
            .Take(request.PageSize + 1)
            .Join(db.SysUnifiedCommands
                .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted),
                o => o.Id,
                c => c.TargetId,
                (o, c) => new OmakaseDto
                {
                    Id = o.Id,
                    Name = c.ResponseText,
                    Count = o.Count,
                    Icon = o.Icon,
                    Price = c.Cost
                })
            .ToListAsync(ct);

        var hasNext = items.Count > request.PageSize;
        if (hasNext) items.RemoveAt(request.PageSize);

        return Result<object>.Success(new { items, hasNext });
    }
}
