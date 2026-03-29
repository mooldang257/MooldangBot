using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Features.Commands.General;

/// <summary>
/// [파로스의 통합]: 명령어 관리 로직을 통합 수행하는 서비스 구현체입니다.
/// </summary>
public class UnifiedCommandService : IUnifiedCommandService
{
    private readonly IAppDbContext _db;
    private readonly ICommandCacheService _cacheService;
    private readonly ILogger<UnifiedCommandService> _logger;

    public UnifiedCommandService(
        IAppDbContext db,
        ICommandCacheService cacheService,
        ILogger<UnifiedCommandService> logger)
    {
        _db = db;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<UnifiedCommand> UpsertCommandAsync(string chzzkUid, SaveUnifiedCommandRequest req)
    {
        var targetUid = chzzkUid.Trim().ToLower();

        UnifiedCommand? entity;
        if (req.Id.HasValue && req.Id.Value > 0)
        {
            entity = await _db.UnifiedCommands
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == req.Id.Value && c.ChzzkUid == targetUid);

            if (entity == null) throw new KeyNotFoundException("수정할 명령어를 찾을 수 없습니다.");
        }
        else
        {
            entity = new UnifiedCommand { ChzzkUid = targetUid, CreatedAt = DateTime.Now };
            _db.UnifiedCommands.Add(entity);
        }

        // 중복 키워드 검사
        int currentId = req.Id ?? 0;
        bool isDuplicate = await _db.UnifiedCommands
            .IgnoreQueryFilters()
            .AnyAsync(c => c.ChzzkUid == targetUid && c.Keyword == req.Keyword && c.Id != currentId);

        if (isDuplicate) throw new InvalidOperationException("이미 존재하는 명령어 키워드입니다.");

        // 데이터 매핑
        entity.Keyword = req.Keyword.Trim();
        entity.Category = Enum.Parse<CommandCategory>(req.Category, true);
        entity.CostType = Enum.Parse<CommandCostType>(req.CostType, true);
        entity.Cost = req.Cost;
        entity.FeatureType = req.FeatureType;
        entity.ResponseText = req.ResponseText;

        // 라이프사이클 사전 처리
        await OnBeforeSaveAsync(entity, req, targetUid);

        entity.IsActive = req.IsActive;
        entity.RequiredRole = Enum.Parse<CommandRole>(req.RequiredRole, true);
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        // 라이프사이클 사후 처리
        bool isNew = (!req.Id.HasValue || req.Id <= 0);
        
        // [v1.9] 통합 룰렛 데이터 처리
        if (req.FeatureType == "Roulette" && req.RouletteData != null)
        {
            await HandleUnifiedRouletteSave(entity, req.RouletteData, targetUid);
            await _db.SaveChangesAsync(); 
        }
        else
        {
            await OnAfterSaveAsync(entity, req, targetUid, isNew);
        }

        // 캐시 갱신
        await _cacheService.RefreshUnifiedAsync(targetUid, default);

        return entity;
    }

    public async Task DeleteCommandAsync(string chzzkUid, int id)
    {
        var targetUid = chzzkUid.Trim().ToLower();
        var entity = await _db.UnifiedCommands
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id && c.ChzzkUid == targetUid);

        if (entity != null)
        {
            // 기능별 삭제 연동 로직 (현재는 오마카세 연동만 존재)
            if (entity.FeatureType == CommandFeatureTypes.Omakase && entity.TargetId.HasValue)
            {
                var itemsToDelete = await _db.StreamerOmakases
                    .IgnoreQueryFilters()
                    .Where(o => o.ChzzkUid == targetUid && o.Id == entity.TargetId.Value)
                    .ToListAsync();

                if (itemsToDelete.Any())
                {
                    _db.StreamerOmakases.RemoveRange(itemsToDelete);
                }
            }

            _db.UnifiedCommands.Remove(entity);
            await _db.SaveChangesAsync();
            await _cacheService.RefreshUnifiedAsync(targetUid, default);
        }
    }

    // --- Private Lifecycle Handlers ---

    private async Task OnBeforeSaveAsync(UnifiedCommand entity, SaveUnifiedCommandRequest req, string targetUid)
    {
        switch (entity.FeatureType)
        {
            case CommandFeatureTypes.Omakase:
                if (entity.Id > 0 && req.TargetId == null) { /* 기존 유지 */ }
                else entity.TargetId = req.TargetId;
                break;
            default:
                entity.TargetId = req.TargetId;
                break;
        }
    }

    private async Task OnAfterSaveAsync(UnifiedCommand entity, SaveUnifiedCommandRequest req, string targetUid, bool isNew)
    {
        if (!isNew) return;

        switch (entity.FeatureType)
        {
            case CommandFeatureTypes.Omakase:
                await HandleOmakaseAfterSave(entity, targetUid);
                break;
            case CommandFeatureTypes.Roulette:
                await HandleRouletteAfterSave(entity, targetUid);
                break;
        }
    }

    private async Task HandleOmakaseAfterSave(UnifiedCommand entity, string targetUid)
    {
        var newItem = new StreamerOmakaseItem { ChzzkUid = targetUid, Icon = "🍣", Count = 0 };
        _db.StreamerOmakases.Add(newItem);
        await _db.SaveChangesAsync();

        entity.TargetId = newItem.Id;
        await _db.SaveChangesAsync();
    }

    private async Task HandleRouletteAfterSave(UnifiedCommand entity, string targetUid)
    {
        var newRoulette = new MooldangBot.Domain.Entities.Roulette
        {
            ChzzkUid = targetUid,
            Name = entity.ResponseText.Length > 0 ? entity.ResponseText : "새 룰렛",
            UpdatedAt = DateTime.UtcNow
        };
        newRoulette.Items.Add(new RouletteItem { ItemName = "당첨!", Probability = 100, Probability10x = 100, IsActive = true });

        _db.Roulettes.Add(newRoulette);
        await _db.SaveChangesAsync();

        entity.TargetId = newRoulette.Id;
        await _db.SaveChangesAsync();
    }

    private async Task HandleUnifiedRouletteSave(UnifiedCommand entity, RouletteSaveDto rouletteData, string targetUid)
    {
        MooldangBot.Domain.Entities.Roulette? roulette = null;
        
        if (entity.TargetId.HasValue && entity.TargetId > 0)
        {
            roulette = await _db.Roulettes.Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == entity.TargetId.Value && r.ChzzkUid == targetUid);
        }

        if (roulette == null)
        {
            roulette = new MooldangBot.Domain.Entities.Roulette { ChzzkUid = targetUid };
            _db.Roulettes.Add(roulette);
        }

        roulette.Name = string.IsNullOrWhiteSpace(rouletteData.Name) ? entity.ResponseText : rouletteData.Name;
        roulette.UpdatedAt = DateTime.UtcNow;

        if (rouletteData.Items != null && rouletteData.Items.Any())
        {
            // 기존 아이템 제거 후 새로 추가
            _db.RouletteItems.RemoveRange(roulette.Items);
            roulette.Items = rouletteData.Items.Select(i => new RouletteItem
            {
                ItemName = i.ItemName,
                Probability = i.Probability,
                Probability10x = i.Probability, // 10연차 확률은 일단 동일하게 복사
                Color = i.Color,
                IsMission = i.IsMission,
                IsActive = i.IsActive
            }).ToList();
        }
        else if (roulette.Items.Count == 0)
        {
            // 데이터가 없는데 신규면 기본값 생성
            roulette.Items.Add(new RouletteItem { ItemName = "당첨!", Probability = 100, Probability10x = 100, IsActive = true });
        }

        await _db.SaveChangesAsync();
        entity.TargetId = roulette.Id;
    }

    public async Task ToggleCommandAsync(string chzzkUid, int id)
    {
        var targetUid = chzzkUid.Trim().ToLower();
        var entity = await _db.UnifiedCommands
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id && c.ChzzkUid == targetUid);

        if (entity != null)
        {
            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.Now;
            
            await _db.SaveChangesAsync();
            await _cacheService.RefreshUnifiedAsync(targetUid, default);
        }
    }
}
