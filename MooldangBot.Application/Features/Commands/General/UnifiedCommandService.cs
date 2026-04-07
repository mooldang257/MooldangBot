using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

using MooldangBot.Domain.Common;

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
        var targetUid = (chzzkUid ?? "").Trim().ToLower();

        // [v4.3] 스트리머 프로필 조회 및 ID 확보
        var streamer = await _db.StreamerProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.ChzzkUid == targetUid);
        if (streamer == null) throw new KeyNotFoundException("스트리머 프로필을 찾을 수 없습니다.");

        // [v4.3] 마스터 기능 조회 (레지스트리 참조로 전환)
        var masterFeature = CommandFeatureRegistry.GetByTypeName(req.FeatureType);
        
        if (masterFeature == null) 
        {
            _logger.LogWarning("⚠️ [UnifiedCommandService]: 정의되지 않은 명령어 기능 타입 요청 (Type: {Type})", req.FeatureType);
            throw new InvalidOperationException($"정의되지 않은 명령어 기능 타입입니다. (Type: {req.FeatureType})");
        }

        UnifiedCommand? entity;
        if (req.Id.HasValue && req.Id.Value > 0)
        {
            entity = await _db.UnifiedCommands
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == req.Id.Value && c.StreamerProfileId == streamer.Id);

            if (entity == null) throw new KeyNotFoundException("수정할 명령어를 찾을 수 없습니다.");
        }
        else
        {
            entity = new UnifiedCommand 
            { 
                StreamerProfileId = streamer.Id, 
                CreatedAt = KstClock.Now 
            };
            _db.UnifiedCommands.Add(entity);
        }

        // 중복 키워드 및 이름 검사
        int currentId = req.Id ?? 0;
        bool isDuplicateKeyword = await _db.UnifiedCommands
            .IgnoreQueryFilters()
            .AnyAsync(c => c.StreamerProfileId == streamer.Id && c.Keyword == req.Keyword && c.Id != currentId);

        if (isDuplicateKeyword) throw new InvalidOperationException("이미 존재하는 명령어 키워드입니다.");

        // [물멍]: 동일한 이름(ResponseText)의 명령어가 이미 존재하는지 체크 (무결성 강화)
        bool isDuplicateName = await _db.UnifiedCommands
            .IgnoreQueryFilters()
            .AnyAsync(c => c.StreamerProfileId == streamer.Id && c.ResponseText == req.ResponseText && c.Id != currentId 
                      && c.FeatureType == masterFeature.Type); // 같은 기능 타입 내에서만 체크

        if (isDuplicateName) throw new InvalidOperationException($"이미 '{req.ResponseText}' 이름의 명령어가 존재합니다.");

        // 데이터 매핑
        entity.Keyword = req.Keyword.Trim();
        entity.FeatureType = masterFeature.Type; // [v4.3] 정문화된 기능 Enum 할당
        entity.CostType = Enum.Parse<CommandCostType>(req.CostType, true);
        entity.Cost = req.Cost;
        entity.ResponseText = req.ResponseText;

        // 라이프사이클 사전 처리
        await OnBeforeSaveAsync(entity, req, targetUid);

        entity.IsActive = req.IsActive; // [v6.1.5] 기능 활성화 (토글)
        entity.IsDeleted = false; // [v6.1.5] 저장/수정 시 존재 상태 보장
        entity.RequiredRole = Enum.Parse<CommandRole>(req.RequiredRole, true);
        entity.UpdatedAt = KstClock.Now;

        await _db.SaveChangesAsync();

        // 라이프사이클 사후 처리
        bool isNew = (!req.Id.HasValue || req.Id <= 0);
        
        if (req.FeatureType == CommandFeatureTypes.Roulette && req.RouletteData != null)
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
        var targetUid = (chzzkUid ?? "").Trim().ToLower();
        var entity = await _db.UnifiedCommands
            .IgnoreQueryFilters()
            .Include(c => c.StreamerProfile)
            .FirstOrDefaultAsync(c => c.Id == id && c.StreamerProfile!.ChzzkUid == targetUid);

        if (entity == null) throw new KeyNotFoundException("삭제할 명령어를 찾을 수 없거나 이미 삭제되었습니다.");

        // [v6.2.5] 자식 엔티티(오마카세 등) 연쇄 삭제 처리 (Soft-Delete 아닌 하드 삭제 수행)
        var featureType = entity.FeatureType.ToString();
        if (featureType == CommandFeatureTypes.Omakase && entity.TargetId.HasValue)
        {
            var itemsToDelete = await _db.StreamerOmakases
                .IgnoreQueryFilters()
                .Where(o => o.StreamerProfileId == entity.StreamerProfileId && o.Id == entity.TargetId.Value)
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

    private async Task OnBeforeSaveAsync(UnifiedCommand entity, SaveUnifiedCommandRequest req, string targetUid)
    {
        var featureType = req.FeatureType; // DTO에서 기능 타입 확인
        switch (featureType)
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
        // [물멍]: 신규 생성이 아니거나, 이미 TargetId가 할당된 경우(컨트롤러에서 직접 생성 등) 건너뜁니다.
        if (!isNew || entity.TargetId.HasValue) return;

        var featureType = req.FeatureType; 
        switch (featureType)
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
        var newItem = new StreamerOmakaseItem { StreamerProfileId = entity.StreamerProfileId, Icon = "🍣", Count = 0 };
        _db.StreamerOmakases.Add(newItem);
        await _db.SaveChangesAsync();

        entity.TargetId = newItem.Id;
        await _db.SaveChangesAsync();
    }

    private async Task HandleRouletteAfterSave(UnifiedCommand entity, string targetUid)
    {
        var newRoulette = new MooldangBot.Domain.Entities.Roulette
        {
            StreamerProfileId = entity.StreamerProfileId, // [v4.4] ChzzkUid 대신 ID 사용
            Name = entity.ResponseText.Length > 0 ? entity.ResponseText : "행운의 룰렛",
            UpdatedAt = KstClock.Now
        };
        newRoulette.Items.Add(new RouletteItem { ItemName = "꽝... 🌧️", Probability = 70, Probability10x = 70, IsActive = true, Color = "#9E9E9E" });
        newRoulette.Items.Add(new RouletteItem { ItemName = "물댕의 축복 ✨", Probability = 20, Probability10x = 20, IsActive = true, Color = "#0093E9" });
        newRoulette.Items.Add(new RouletteItem { ItemName = "대박 당첨! 💎", Probability = 10, Probability10x = 10, IsActive = true, Color = "#FF9A9E" });

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
                .Include(r => r.StreamerProfile)
                .FirstOrDefaultAsync(r => r.Id == entity.TargetId.Value && r.StreamerProfileId == entity.StreamerProfileId);
        }

        if (roulette == null)
        {
            roulette = new MooldangBot.Domain.Entities.Roulette { StreamerProfileId = entity.StreamerProfileId };
            _db.Roulettes.Add(roulette);
        }

        roulette.Name = string.IsNullOrWhiteSpace(rouletteData.Name) ? entity.ResponseText : rouletteData.Name;
        roulette.UpdatedAt = KstClock.Now;

        if (rouletteData.Items != null && rouletteData.Items.Any())
        {
            _db.RouletteItems.RemoveRange(roulette.Items);
            roulette.Items = rouletteData.Items.Select(i => new RouletteItem
            {
                ItemName = i.ItemName,
                Probability = i.Probability,
                Probability10x = i.Probability,
                Color = i.Color,
                IsMission = i.IsMission,
                IsActive = i.IsActive // [v6.1.5] 하위 테이블은 활동성(Active)으로만 관리
            }).ToList();
        }
        else if (roulette.Items.Count == 0)
        {
            // 데이터가 없는데 신규면 기본값 생성
            roulette.Items.Add(new RouletteItem { ItemName = "꽝... 🌧️", Probability = 70, Probability10x = 70, IsActive = true, Color = "#9E9E9E" });
            roulette.Items.Add(new RouletteItem { ItemName = "물댕의 축복 ✨", Probability = 20, Probability10x = 20, IsActive = true, Color = "#0093E9" });
            roulette.Items.Add(new RouletteItem { ItemName = "대박 당첨! 💎", Probability = 10, Probability10x = 10, IsActive = true, Color = "#FF9A9E" });
        }
        await _db.SaveChangesAsync();
        entity.TargetId = roulette.Id;
    }

    public async Task ToggleCommandAsync(string chzzkUid, int id)
    {
        var targetUid = (chzzkUid ?? "").Trim().ToLower();
        var entity = await _db.UnifiedCommands
            .IgnoreQueryFilters()
            .Include(c => c.StreamerProfile)
            .FirstOrDefaultAsync(c => c.Id == id && c.StreamerProfile!.ChzzkUid == targetUid);

        if (entity != null)
        {
            entity.IsActive = !entity.IsActive; // [v6.1.5] 토글은 활동 상태(Active)를 반전
            entity.UpdatedAt = KstClock.Now;
            await _db.SaveChangesAsync();
            await _cacheService.RefreshUnifiedAsync(targetUid, default);
        }
    }

    /// <summary>
    /// [파로스의 시작]: 신규 가입한 스트리머에게 필수적인 기본 명령어 세트를 자동 생성합니다.
    /// </summary>
    public async Task InitializeDefaultCommandsAsync(string chzzkUid)
    {
        // [물멍의 지리]: UID는 캐시 및 조회 일관성을 위해 항상 소문자로 정규화하여 처리합니다.
        var targetUid = (chzzkUid ?? "").Trim().ToLower();
        _logger.LogInformation("🌱 [CommandSeeder]: 스트리머({Uid})를 위한 기본 명령어 생성을 시작합니다.", targetUid);

        var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(s => s.ChzzkUid == targetUid);
        if (streamer == null) return;

        int addedCount = 0;

        // 1. [기능] 노래 신청 (치즈 1000)
        addedCount += await EnsureCommandAsync(streamer, "!신청", CommandCategory.Feature, CommandCostType.Cheese, 1000, CommandFeatureTypes.SongRequest, "신청곡 룰렛", CommandRole.Viewer);
        
        // 2. [기능] 룰렛 (치즈 1000)
        addedCount += await EnsureCommandAsync(streamer, "!룰렛", CommandCategory.Feature, CommandCostType.Cheese, 1000, CommandFeatureTypes.Roulette, "행운의 룰렛", CommandRole.Viewer);

        // 3. [기능] 출석 (무료, 보상 10P)
        addedCount += await EnsureCommandAsync(streamer, "!출석", CommandCategory.Feature, CommandCostType.None, 10, CommandFeatureTypes.Attendance, "{닉네임}님 출석 고마워요! 현재 {출석일수}일차이며 {포인트}포인트를 보유 중입니다.", CommandRole.Viewer);

        // 4. [일반] 포인트 확인 (무료)
        addedCount += await EnsureCommandAsync(streamer, "!포인트", CommandCategory.General, CommandCostType.None, 0, CommandFeatureTypes.Reply, "🪙 {닉네임}님의 보유 포인트는 {포인트}점입니다! (누적 출석: {출석일수}일)", CommandRole.Viewer);

        // 5. [시스템] 송리스트 토글 (매니저)
        addedCount += await EnsureCommandAsync(streamer, "!송리스트", CommandCategory.System, CommandCostType.None, 0, CommandFeatureTypes.SonglistToggle, "송리스트 상태 변경", CommandRole.Manager);

        // 6. [시스템] 방송 관리 3종 (매니저)
        addedCount += await EnsureCommandAsync(streamer, "!공지", CommandCategory.System, CommandCostType.None, 0, CommandFeatureTypes.Notice, "공지사항", CommandRole.Manager);
        addedCount += await EnsureCommandAsync(streamer, "!방제", CommandCategory.System, CommandCostType.None, 0, CommandFeatureTypes.Title, "제목 변경", CommandRole.Manager);
        addedCount += await EnsureCommandAsync(streamer, "!카테고리", CommandCategory.System, CommandCostType.None, 0, CommandFeatureTypes.Category, "카테고리 변경", CommandRole.Manager);

        if (addedCount > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("✅ [CommandSeeder]: {Count}개의 기본 명령어가 생성되었습니다.", addedCount);
            
            // 캐시 갱신
            await _cacheService.RefreshUnifiedAsync(targetUid, default);
        }
    }

    private async Task<int> EnsureCommandAsync(StreamerProfile streamer, string keyword, CommandCategory cat, CommandCostType costType, int cost, string feature, string response, CommandRole role)
    {
        // 멱등성 보장: 이미 해당 키워드가 존재하면 스킵
        bool exists = await _db.UnifiedCommands
            .IgnoreQueryFilters()
            .AnyAsync(c => c.StreamerProfileId == streamer.Id && c.Keyword == keyword);

        if (exists) return 0;

        // 마스터 기능 조회 (레지스트리 참조)
        var masterFeature = CommandFeatureRegistry.GetByTypeName(feature);
        
        if (masterFeature == null) return 0;

        var entity = new UnifiedCommand
        {
            StreamerProfileId = streamer.Id,
            Keyword = keyword,
            FeatureType = masterFeature.Type,
            CostType = costType,
            Cost = cost,
            ResponseText = response,
            RequiredRole = role,
            IsActive = true, // [v6.1.5] 신규 기본 명령어는 활성화 상태
            IsDeleted = false, 
            CreatedAt = KstClock.Now
        };

        _db.UnifiedCommands.Add(entity);

        // 사후 처리 (오마카세/룰렛 초기화 등)
        var req = new SaveUnifiedCommandRequest(null, keyword, cat.ToString(), costType.ToString(), cost, feature, response, null, true, role.ToString(), null);
        await OnAfterSaveAsync(entity, req, streamer.ChzzkUid, true);
        
        return 1;
    }
}
