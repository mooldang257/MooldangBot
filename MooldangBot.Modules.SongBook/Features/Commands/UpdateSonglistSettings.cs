using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Models;
using MooldangBot.Contracts.Commands.Interfaces;
using MooldangBot.Contracts.SongBook.DTOs;
using MooldangBot.Modules.SongBookModule.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Modules.SongBookModule.Features.Commands;

/// <summary>
/// [하모니의 조율]: 송북 설정 및 관련 명령어들을 통합적으로 동기화합니다.
/// </summary>
public record UpdateSonglistSettingsCommand(string StreamerUid, MooldangBot.Contracts.SongBook.DTOs.SonglistSettingsUpdateRequest Request) : IRequest<Result<object>>;

public class UpdateSonglistSettingsHandler(
    ISongBookDbContext db, 
    IUnifiedCommandService unifiedCommandService) : IRequestHandler<UpdateSonglistSettingsCommand, Result<object>>
{
    public async Task<Result<object>> Handle(UpdateSonglistSettingsCommand request, CancellationToken ct)
    {
        var targetUid = request.StreamerUid.ToLower();
        var profile = await db.StreamerProfiles
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == targetUid && !p.IsDeleted, ct);
            
        if (profile == null) 
            return Result<object>.Failure("존재하지 않는 채널입니다.");

        profile.DesignSettingsJson = request.Request.DesignSettingsJson;

        // 1. Omakase Items Sync
        var existingItems = await db.StreamerOmakases
            .Where(o => o.StreamerProfileId == profile.Id)
            .Where(o => db.UnifiedCommands.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted))
            .ToListAsync(ct);

        if (request.Request.Omakases != null)
        {
            var processedIds = new HashSet<int>();

            foreach (var dto in request.Request.Omakases)
            {
                var item = existingItems.FirstOrDefault(x => x.Id == dto.Id && dto.Id > 0);
                if (item == null)
                {
                    item = new StreamerOmakaseItem
                    {
                        StreamerProfileId = profile.Id,
                        Icon = dto.Icon,
                        Count = 0
                    };
                    db.StreamerOmakases.Add(item);
                }
                else
                {
                    item.Icon = dto.Icon;
                }

                await db.SaveChangesAsync(ct);
                processedIds.Add(item.Id);
                dto.Id = item.Id;
            }

            var toDelete = existingItems.Where(e => !processedIds.Contains(e.Id));
            db.StreamerOmakases.RemoveRange(toDelete);
        }

        // Sync Commands
        var existingCmds = await db.UnifiedCommands
            .Where(c => c.StreamerProfileId == profile.Id && (c.FeatureType == CommandFeatureType.SongRequest || c.FeatureType == CommandFeatureType.Omakase) && !c.IsDeleted)
            .ToListAsync(ct);

        var features = CommandFeatureRegistry.All;
        var songMaster = features.FirstOrDefault(f => f.Type == CommandFeatureType.SongRequest);
        var omakaseMaster = features.FirstOrDefault(f => f.Type == CommandFeatureType.Omakase);

        // 1. SongRequest Upsert
        if (request.Request.SongRequestCommands != null)
        {
            foreach (var sc in request.Request.SongRequestCommands)
            {
                if (string.IsNullOrWhiteSpace(sc.Keyword)) continue;
                
                var existing = existingCmds.FirstOrDefault(c => c.Keyword == sc.Keyword && c.FeatureType == CommandFeatureType.SongRequest);
                
                await unifiedCommandService.UpsertCommandAsync(targetUid, new SaveUnifiedCommandRequest(
                    Id: existing?.Id,
                    Keyword: sc.Keyword.Trim(),
                    Category: CommandCategory.Feature.ToString(),
                    CostType: CommandCostType.Cheese.ToString(),
                    Cost: sc.Price,
                    FeatureType: CommandFeatureTypes.SongRequest,
                    ResponseText: string.IsNullOrWhiteSpace(sc.Name) ? "노래 신청" : sc.Name.Trim(),
                    TargetId: null,
                    IsActive: true,
                    RequiredRole: (songMaster?.RequiredRole ?? CommandRole.Viewer).ToString()
                ));
            }
        }

        // 2. Omakase Upsert
        if (request.Request.Omakases != null && request.Request.Omakases.Any())
        {
            var uniqueKeywords = request.Request.Omakases
                .Where(o => !string.IsNullOrWhiteSpace(o.Command))
                .GroupBy(o => o.Command.Trim())
                .Select(g => new { Keyword = g.Key, First = g.First() })
                .ToList();

            foreach (var uk in uniqueKeywords)
            {
                var existing = existingCmds.FirstOrDefault(c => string.Equals(c.Keyword, uk.Keyword, StringComparison.OrdinalIgnoreCase) && c.FeatureType == CommandFeatureType.Omakase);

                await unifiedCommandService.UpsertCommandAsync(targetUid, new SaveUnifiedCommandRequest(
                    Id: existing?.Id,
                    Keyword: uk.Keyword.Trim(),
                    Category: CommandCategory.Feature.ToString(),
                    CostType: CommandCostType.Cheese.ToString(),
                    Cost: uk.First.Price,
                    FeatureType: CommandFeatureTypes.Omakase,
                    ResponseText: uk.First.Name.Trim(),
                    TargetId: uk.First.Id,
                    IsActive: true,
                    RequiredRole: (omakaseMaster?.RequiredRole ?? CommandRole.Viewer).ToString()
                ));
            }
        }

        var incomingKeywords = (request.Request.SongRequestCommands?.Select(s => s.Keyword.Trim()) ?? Enumerable.Empty<string>())
            .Concat(request.Request.Omakases?.Select(o => o.Command?.Trim()).Where(k => !string.IsNullOrEmpty(k)) ?? Enumerable.Empty<string>())
            .ToList();

        var toRemove = existingCmds.Where(e => !incomingKeywords.Any(k => string.Equals(k, e.Keyword, StringComparison.OrdinalIgnoreCase)));
        foreach (var tr in toRemove)
        {
            await unifiedCommandService.DeleteCommandAsync(targetUid, tr.Id);
        }

        await db.SaveChangesAsync(ct);
        return Result<object>.Success(new { success = true });
    }
}
