using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Modules.SongBookModule.Abstractions;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.SongBookModule.Features.Queries;

/// <summary>
/// [?ㅼ젙 ?쇳꽣: ?〓턿]: ?ㅽ듃由щ㉧???〓턿 ?ㅼ젙 ?곗씠??紐낅졊?? ?ㅻ쭏移댁꽭, ?붿옄????瑜?議고쉶?⑸땲??
/// </summary>
public record GetSonglistSettingsDataQuery(string StreamerUid) : IRequest<Result<object>>;

public class GetSonglistSettingsDataHandler(ISongBookDbContext db) : IRequestHandler<GetSonglistSettingsDataQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetSonglistSettingsDataQuery request, CancellationToken ct)
    {
        var targetUid = request.StreamerUid.ToLower();
        var profile = await db.StreamerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid && !p.IsDeleted, ct);
        
        if (profile == null) 
            return Result<object>.Failure("議댁옱?섏? ?딅뒗 梨꾨꼸?낅땲??");

        var omakaseItems = await db.StreamerOmakases.AsNoTracking()
            .Where(o => o.StreamerProfileId == profile.Id)
            .Where(o => db.UnifiedCommands.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted))
            .ToListAsync(ct);

        var songCommands = await db.UnifiedCommands
            .AsNoTracking()
            .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.SongRequest && !c.IsDeleted)
            .Select(c => new { Keyword = c.Keyword, Price = c.Cost, Name = c.ResponseText })
            .ToListAsync(ct);

        var omakaseCommands = await db.UnifiedCommands
            .AsNoTracking()
            .Where(c => c.StreamerProfileId == profile.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted)
            .ToListAsync(ct);

        var result = new
        {
            songCommand = "!?좎껌", 
            songRequestCommands = songCommands.Select(c => new { 
                trigger = c.Keyword, 
                cost = c.Price, 
                name = c.Name 
            }),
            songPrice = 0,
            designSettingsJson = profile.DesignSettingsJson,
            omakases = omakaseItems
                .Select(o => {
                    var cmd = omakaseCommands.FirstOrDefault(c => c.TargetId == o.Id);
                    return new {
                        id = o.Id,
                        name = cmd?.ResponseText ?? "???ㅻ쭏移댁꽭",
                        trigger = cmd?.Keyword ?? "", 
                        icon = o.Icon,
                        cost = cmd?.Cost ?? 0,
                        targetId = o.Id,
                        count = o.Count
                    };
                }),
            labels = TryGetLabels(profile.DesignSettingsJson)
        };

        return Result<object>.Success(result);
    }

    private object TryGetLabels(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new { nowPlaying = "??NOW PLAYING", upNext = "??UP NEXT", completed = "??COMPLETED" };
        try {
            var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Labels", out var labels)) {
                return labels;
            }
        } catch {}
        return new { nowPlaying = "??NOW PLAYING", upNext = "??UP NEXT", completed = "??COMPLETED" };
    }
}
