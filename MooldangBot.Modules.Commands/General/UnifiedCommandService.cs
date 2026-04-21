using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

using MooldangBot.Domain.Common;

namespace MooldangBot.Modules.Commands.General;

/// <summary>
/// [파로스의 통합]: 명령어 관리 로직을 통합 수행하는 서비스 구현체입니다.
/// </summary>
public class UnifiedCommandService : IUnifiedCommandService
{
    private readonly ICommandDbContext _db;
    private readonly ICommandCache _cacheService;
    private readonly ILogger<UnifiedCommandService> _logger;

    public UnifiedCommandService(
        ICommandDbContext db,
        ICommandCache cacheService,
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

        // [물멍]: 다중 타격(Multicasting) 전술을 위해 동일 키워드로 여러 명령 중복 등록을 허용합니다. (키워드 중복 검사 제거)

        // [물멍]: 다중 타격(Multicasting) 전술을 위해 메시지의 중복은 허용합니다. (중복 이름 검사 제거)

        // [v2.4.7] 오시리스의 관용: Enum 파싱 시 규격 외 값이 들어와도 안전한 기본값으로 폴백합니다.
        entity.Keyword = req.Keyword.Trim();
        
        if (Enum.TryParse<CommandMatchType>(req.MatchType, true, out var matchType))
            entity.MatchType = matchType;
        else
            entity.MatchType = CommandMatchType.Prefix;

        entity.RequiresSpace = req.RequiresSpace;
        entity.FeatureType = masterFeature.Type;

        if (Enum.TryParse<CommandCostType>(req.CostType, true, out var costType))
            entity.CostType = costType;
        else
            entity.CostType = CommandCostType.None;

        entity.Cost = req.Cost;
        entity.ResponseText = req.ResponseText;

        // 라이프사이클 사전 처리
        await OnBeforeSaveAsync(entity, req, targetUid);

        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;

        if (Enum.TryParse<CommandRole>(req.RequiredRole, true, out var role))
            entity.RequiredRole = role;
        else
            entity.RequiredRole = CommandRole.Viewer;

        entity.Priority = req.Priority;
        entity.UpdatedAt = KstClock.Now;

        await _db.SaveChangesAsync(default); // [v6.2] CancellationToken 보강

        // 라이프사이클 사후 처리
        bool isNew = (!req.Id.HasValue || req.Id <= 0);
        
        if (req.FeatureType == CommandFeatureTypes.Roulette && req.RouletteData != null)
        {
            await HandleUnifiedRouletteSave(entity, req.RouletteData, targetUid);
            await _db.SaveChangesAsync(default); 
        }
        else
        {
            await OnAfterSaveAsync(entity, req, targetUid, isNew);
        }

        // 캐시 갱신 (ICommandCache 사용)
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

        // [물멍]: 자식 엔티티(오마카세 등)는 연쇄 삭제하지 않고 보존합니다. (선장님 피드백 반영: 데이터 영속성 유지)

        _db.UnifiedCommands.Remove(entity);
        await _db.SaveChangesAsync(default);
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
        await _db.SaveChangesAsync(default);

        entity.TargetId = newItem.Id;
        await _db.SaveChangesAsync(default);
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
        await _db.SaveChangesAsync(default);

        entity.TargetId = newRoulette.Id;
        await _db.SaveChangesAsync(default);
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
        await _db.SaveChangesAsync(default);
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
            await _db.SaveChangesAsync(default);
            await _cacheService.RefreshUnifiedAsync(targetUid, default);
        }
    }

    /// <summary>
    /// [파로스의 시작]: 신규 가입한 스트리머에게 필수적인 기본 명령어 세트를 자동 생성합니다.
    /// </summary>
    public async Task InitializeDefaultCommandsAsync(string chzzkUid)
    {
        var targetUid = (chzzkUid ?? "").Trim().ToLower();
        _logger.LogInformation("🌱 [CommandSeeder]: 스트리머({Uid})를 위한 기본 명령어 생성을 시작합니다.", targetUid);

        var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(s => s.ChzzkUid == targetUid);
        if (streamer == null) return;

        // [물멍의 지혜]: 존재 여부를 한 번의 쿼리로 대량 확인하여 N+1 문제를 방지합니다.
        var existingKeywords = await _db.UnifiedCommands
            .IgnoreQueryFilters()
            .Where(c => c.StreamerProfileId == streamer.Id)
            .Select(c => c.Keyword)
            .ToListAsync();

        var keywordsToSeed = new[] { "!신청", "!룰렛", "!출석", "!포인트", "!송리스트", "!공지", "!방제", "!카테고리" };
        var missingKeywords = keywordsToSeed.Except(existingKeywords).ToList();

        if (!missingKeywords.Any())
        {
            _logger.LogInformation("✅ [CommandSeeder]: 이미 모든 기본 명령어가 존재합니다.");
            return;
        }

        var createdEntities = new List<(UnifiedCommand Entity, string Feature)>();

        foreach (var keyword in missingKeywords)
        {
            var entity = CreateDefaultCommandEntity(streamer, keyword);
            if (entity != null)
            {
                _db.UnifiedCommands.Add(entity);
                
                // 어떤 기능인지 추후 사후 처리를 위해 보관
                var feature = GetFeatureByKeyword(keyword);
                createdEntities.Add((entity, feature));
            }
        }

        // [1차 저장]: 명령어 엔티티들을 먼저 저장하여 ID를 확보합니다.
        await _db.SaveChangesAsync(default);

        // [2차 처리]: 생성된 ID를 기반으로 룰렛/오마카세 등 연결 엔티티 생성
        bool needsSecondSave = false;
        foreach (var (entity, feature) in createdEntities)
        {
            if (feature == CommandFeatureTypes.Roulette)
            {
                var roulette = CreateDefaultRoulette(streamer.Id, entity.ResponseText);
                _db.Roulettes.Add(roulette);
                await _db.SaveChangesAsync(default); // 각 룰렛 ID가 필요하므로 어쩔 수 없이 저장
                entity.TargetId = roulette.Id;
                needsSecondSave = true;
            }
            else if (feature == CommandFeatureTypes.Omakase)
            {
                var omakase = new StreamerOmakaseItem { StreamerProfileId = streamer.Id, Icon = "🍣", Count = 0 };
                _db.StreamerOmakases.Add(omakase);
                await _db.SaveChangesAsync(default);
                entity.TargetId = omakase.Id;
                needsSecondSave = true;
            }
        }

        if (needsSecondSave)
        {
            await _db.SaveChangesAsync(default);
        }

        _logger.LogInformation("✅ [CommandSeeder]: {Count}개의 기본 명령어가 성공적으로 생성 및 연결되었습니다.", createdEntities.Count);
        await _cacheService.RefreshUnifiedAsync(targetUid, default);
    }

    private string GetFeatureByKeyword(string keyword) => keyword switch
    {
        "!신청" => CommandFeatureTypes.SongRequest,
        "!룰렛" => CommandFeatureTypes.Roulette,
        "!출석" => CommandFeatureTypes.Attendance,
        "!송리스트" => CommandFeatureTypes.SonglistToggle,
        "!공지" => CommandFeatureTypes.Notice,
        "!방제" => CommandFeatureTypes.Title,
        "!카테고리" => CommandFeatureTypes.Category,
        _ => CommandFeatureTypes.Reply
    };

    private UnifiedCommand? CreateDefaultCommandEntity(StreamerProfile streamer, string keyword)
    {
        string feature = GetFeatureByKeyword(keyword);
        var masterFeature = CommandFeatureRegistry.GetByTypeName(feature);
        if (masterFeature == null) return null;

        var (costType, cost, response, role) = keyword switch
        {
            "!신청" => (CommandCostType.Cheese, 1000, "신청곡 룰렛", CommandRole.Viewer),
            "!룰렛" => (CommandCostType.Cheese, 1000, "행운의 룰렛", CommandRole.Viewer),
            "!출석" => (CommandCostType.None, 10, "{닉네임}님 출석 고마워요! 현재 {출석일수}일차이며 {포인트}포인트를 보유 중입니다.", CommandRole.Viewer),
            "!포인트" => (CommandCostType.None, 0, "🪙 {닉네임}님의 보유 포인트는 {포인트}점입니다! (누적 출석: {출석일수}일)", CommandRole.Viewer),
            "!송리스트" => (CommandCostType.None, 0, "송리스트 상태 변경", CommandRole.Manager),
            "!공지" => (CommandCostType.None, 0, "공지사항", CommandRole.Manager),
            "!방제" => (CommandCostType.None, 0, "제목 변경", CommandRole.Manager),
            "!카테고리" => (CommandCostType.None, 0, "카테고리 변경", CommandRole.Manager),
            _ => (CommandCostType.None, 0, "", CommandRole.Viewer)
        };

        return new UnifiedCommand
        {
            StreamerProfileId = streamer.Id,
            Keyword = keyword,
            FeatureType = masterFeature.Type,
            CostType = costType,
            Cost = cost,
            ResponseText = response,
            RequiredRole = role,
            MatchType = CommandMatchType.Prefix,
            RequiresSpace = true,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = KstClock.Now
        };
    }

    private MooldangBot.Domain.Entities.Roulette CreateDefaultRoulette(int streamerProfileId, string name)
    {
        var roulette = new MooldangBot.Domain.Entities.Roulette
        {
            StreamerProfileId = streamerProfileId,
            Name = string.IsNullOrWhiteSpace(name) ? "행운의 룰렛" : name,
            UpdatedAt = KstClock.Now
        };
        roulette.Items.Add(new RouletteItem { ItemName = "꽝... 🌧️", Probability = 70, Probability10x = 70, IsActive = true, Color = "#9E9E9E" });
        roulette.Items.Add(new RouletteItem { ItemName = "물댕의 축복 ✨", Probability = 20, Probability10x = 20, IsActive = true, Color = "#0093E9" });
        roulette.Items.Add(new RouletteItem { ItemName = "대박 당첨! 💎", Probability = 10, Probability10x = 10, IsActive = true, Color = "#FF9A9E" });
        return roulette;
    }
}

