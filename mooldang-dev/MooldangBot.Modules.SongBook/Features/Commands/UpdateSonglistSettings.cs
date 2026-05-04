using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Modules.SongBook.Features.Commands;

/// <summary>
/// [하모니의 조율]: 송북 설정 및 관련 명령어들을 통합적으로 동기화합니다.
/// </summary>
public record UpdateSonglistSettingsCommand(string StreamerUid, MooldangBot.Domain.Contracts.SongBook.SonglistSettingsUpdateRequest Request) : IRequest<Result<object>>;

public class UpdateSonglistSettingsHandler(
    ISongBookDbContext db, 
    IUnifiedCommandService unifiedCommandService) : IRequestHandler<UpdateSonglistSettingsCommand, Result<object>>
{
    public async Task<Result<object>> Handle(UpdateSonglistSettingsCommand request, CancellationToken ct)
    {
        var TargetUid = request.StreamerUid.ToLower();
        var Profile = await db.TableCoreStreamerProfiles
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == TargetUid && !p.IsDeleted, ct);
            
        if (Profile == null) 
            return Result<object>.Failure("존재하지 않는 채널입니다.");
 
        Profile.DesignSettingsJson = request.Request.DesignSettingsJson;
 
        // 1. Omakase Items Sync
        var ExistingItems = await db.TableFuncSongListOmakases
            .Where(o => o.StreamerProfileId == Profile.Id)
            .Where(o => db.TableFuncCmdUnified.Any(c => c.TargetId == o.Id && c.FeatureType == CommandFeatureType.Omakase && !c.IsDeleted))
            .ToListAsync(ct);

        if (request.Request.Omakases != null)
        {
            var ProcessedIds = new HashSet<int>();
 
            foreach (var Dto in request.Request.Omakases)
            {
                var Item = ExistingItems.FirstOrDefault(x => x.Id == Dto.Id && Dto.Id > 0);
                if (Item == null)
                {
                    Item = new FuncSongListOmakases
                    {
                        StreamerProfileId = Profile.Id,
                        Icon = Dto.Icon,
                        Count = 0
                    };
                    db.TableFuncSongListOmakases.Add(Item);
                }
                else
                {
                    Item.Icon = Dto.Icon;
                }
 
                await db.SaveChangesAsync(ct);
                ProcessedIds.Add(Item.Id);
                Dto.Id = Item.Id;
            }
 
            var ToDelete = ExistingItems.Where(e => !ProcessedIds.Contains(e.Id));
            db.TableFuncSongListOmakases.RemoveRange(ToDelete);
        }

        // Sync Commands
        var ExistingCmds = await db.TableFuncCmdUnified
            .Where(c => c.StreamerProfileId == Profile.Id && (c.FeatureType == CommandFeatureType.SongRequest || c.FeatureType == CommandFeatureType.Omakase) && !c.IsDeleted)
            .ToListAsync(ct);
 
        var Features = CommandFeatureRegistry.All;
        var SongMaster = Features.FirstOrDefault(f => f.Type == CommandFeatureType.SongRequest);
        var OmakaseMaster = Features.FirstOrDefault(f => f.Type == CommandFeatureType.Omakase);
 
        // 1. SongRequest Upsert
        if (request.Request.SongRequestCommands != null)
        {
            foreach (var Sc in request.Request.SongRequestCommands)
            {
                if (string.IsNullOrWhiteSpace(Sc.Keyword)) continue;
                
                var Existing = ExistingCmds.FirstOrDefault(c => c.Keyword == Sc.Keyword && c.FeatureType == CommandFeatureType.SongRequest);
                
                await unifiedCommandService.UpsertCommandAsync(TargetUid, new SaveUnifiedCommandRequest(
                    Id: Existing?.Id,
                    Keyword: Sc.Keyword.Trim(),
                    Category: CommandCategory.Feature.ToString(),
                    CostType: CommandCostType.Cheese.ToString(),
                    Cost: Sc.Price,
                    FeatureType: CommandFeatureTypes.SongRequest,
                    ResponseText: string.IsNullOrWhiteSpace(Sc.Name) ? "노래 신청" : Sc.Name.Trim(),
                    TargetId: null,
                    IsActive: true,
                    RequiredRole: (SongMaster?.RequiredRole ?? CommandRole.Viewer).ToString()
                ));
            }
        }

        // 2. Omakase Upsert
        if (request.Request.Omakases != null && request.Request.Omakases.Any())
        {
            var UniqueKeywords = request.Request.Omakases
                .Where(o => !string.IsNullOrWhiteSpace(o.Command))
                .GroupBy(o => o.Command.Trim())
                .Select(g => new { Keyword = g.Key, First = g.First() })
                .ToList();
 
            foreach (var Uk in UniqueKeywords)
            {
                var Existing = ExistingCmds.FirstOrDefault(c => string.Equals(c.Keyword, Uk.Keyword, StringComparison.OrdinalIgnoreCase) && c.FeatureType == CommandFeatureType.Omakase);
 
                await unifiedCommandService.UpsertCommandAsync(TargetUid, new SaveUnifiedCommandRequest(
                    Id: Existing?.Id,
                    Keyword: Uk.Keyword.Trim(),
                    Category: CommandCategory.Feature.ToString(),
                    CostType: CommandCostType.Cheese.ToString(),
                    Cost: Uk.First.Price,
                    FeatureType: CommandFeatureTypes.Omakase,
                    ResponseText: Uk.First.Name.Trim(),
                    TargetId: Uk.First.Id,
                    IsActive: true,
                    RequiredRole: (OmakaseMaster?.RequiredRole ?? CommandRole.Viewer).ToString()
                ));
            }
        }

        var IncomingKeywords = (request.Request.SongRequestCommands?.Select(s => s.Keyword.Trim()) ?? Enumerable.Empty<string>())
            .Concat(request.Request.Omakases?.Select(o => o.Command?.Trim()).Where(k => !string.IsNullOrEmpty(k)) ?? Enumerable.Empty<string>())
            .ToList();
 
        var ToRemove = ExistingCmds.Where(e => !IncomingKeywords.Any(k => string.Equals(k, e.Keyword, StringComparison.OrdinalIgnoreCase)));
        foreach (var Tr in ToRemove)
        {
            await unifiedCommandService.DeleteCommandAsync(TargetUid, Tr.Id);
        }
 
        await db.SaveChangesAsync(ct);
        return Result<object>.Success(new { Success = true });
    }
}
