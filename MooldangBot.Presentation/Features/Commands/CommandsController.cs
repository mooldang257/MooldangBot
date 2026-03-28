using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;

namespace MooldangBot.Presentation.Features.Commands
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")] // 🛡️ 모든 명령어 관리에 채널 매니저 정책 적용
    public class CommandsController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly ICommandCacheService _cacheService;
        private readonly ICommandMasterCacheService _masterCache; // [v1.2] 마스터 캐시 추가
        private readonly ILogger<CommandsController> _logger;

        public CommandsController(
            IAppDbContext db, 
            ICommandCacheService cacheService, 
            ICommandMasterCacheService masterCache, // [v1.2] 주입
            ILogger<CommandsController> logger)
        {
            _db = db;
            _cacheService = cacheService;
            _masterCache = masterCache;
            _logger = logger;
        }


        /// <summary>
        /// [v1.5] 통합 명령어 목록 조회 (오프셋 기반 인풋 페이징)
        /// </summary>
        [HttpGet("/api/commands/unified/{chzzkUid}")]
        public async Task<IResult> GetUnifiedCommands(
            string chzzkUid, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            var targetUid = chzzkUid.Trim().ToLower();
            
            // 🔍 기본 쿼리 빌드 (Id 내림차순 정렬)
            var query = _db.UnifiedCommands
                .IgnoreQueryFilters()
                .Where(c => c.ChzzkUid == targetUid)
                .OrderByDescending(c => c.Id);

            // 전체 카운트는 인풋 페이징의 totalPages 계산을 위해 필수적임
            int totalCount = await query.CountAsync();

            // 🚀 오프셋 페이징 (성능을 위해 인덱스 컬럼만 우선 조회하는 등의 고도화 가능하나 현재는 표준 Skip/Take 사용)
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new UnifiedCommandDto(
                    c.Id, c.Keyword, c.Category.ToString(), c.CostType.ToString(), 
                    c.Cost, c.FeatureType, c.ResponseText, c.TargetId, c.IsActive,
                    c.RequiredRole.ToString()))
                .ToListAsync();

            return Results.Ok(new UnifiedPagedResponse<UnifiedCommandDto>(items, totalCount, page, pageSize));
        }

        /// <summary>
        /// [v1.6] 통합 명령어 저장 또는 수정 (Upsert 패턴)
        /// </summary>
        [HttpPost("/api/commands/unified/{chzzkUid}")]
        public async Task<IResult> UpsertUnifiedCommand(string chzzkUid, [FromBody] SaveUnifiedCommandRequest req)
        {
            var targetUid = chzzkUid.Trim().ToLower();
            
            UnifiedCommand? entity;
            if (req.Id.HasValue && req.Id.Value > 0)
            {
                // 수정: 기존 엔티티 조회
                entity = await _db.UnifiedCommands
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == req.Id.Value && c.ChzzkUid == targetUid);
                
                if (entity == null) return Results.NotFound("수정할 명령어를 찾을 수 없습니다.");
            }
            else
            {
                // 신규: 엔티티 생성 및 추가
                entity = new UnifiedCommand { ChzzkUid = targetUid, CreatedAt = DateTime.Now };
                _db.UnifiedCommands.Add(entity);
            }

            // [v1.6-Refine] 중복 키워드 검사 (Unique Index 충돌 방어)
            int currentId = req.Id ?? 0;
            bool isDuplicate = await _db.UnifiedCommands
                .IgnoreQueryFilters()
                .AnyAsync(c => c.ChzzkUid == targetUid && c.Keyword == req.Keyword && c.Id != currentId);

            if (isDuplicate) 
                return Results.BadRequest(new { Message = "이미 존재하는 명령어 키워드입니다. (Osiris's Warning) ⚠️" });

            // 속성 업데이트 (DTO -> Entity)
            entity.Keyword = req.Keyword;
            entity.Category = Enum.Parse<CommandCategory>(req.Category, true);
            entity.CostType = Enum.Parse<CommandCostType>(req.CostType, true);
            entity.Cost = req.Cost;
            entity.FeatureType = req.FeatureType;
            entity.ResponseText = req.ResponseText;
            entity.TargetId = req.TargetId;
            entity.IsActive = req.IsActive;
            entity.RequiredRole = Enum.Parse<CommandRole>(req.RequiredRole, true);
            entity.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            
            // 캐시 무효화 (실시간 반영)
            await _cacheService.RefreshUnifiedAsync(targetUid, default);

            return Results.Ok(new { Message = req.Id > 0 ? "수정 완료" : "생성 완료", Id = entity.Id });
        }

        // [레거시 지원] /save/ 경로도 신규 로직으로 연동
        [HttpPost("/api/commands/unified/save/{chzzkUid}")]
        public async Task<IResult> SaveUnifiedCommand(string chzzkUid, [FromBody] SaveUnifiedCommandRequest req) 
            => await UpsertUnifiedCommand(chzzkUid, req);

        [HttpDelete("/api/commands/unified/delete/{chzzkUid}/{id}")]
        public async Task<IResult> DeleteUnifiedCommand(string chzzkUid, int id)
        {
            var targetUid = chzzkUid.Trim().ToLower();
            var entity = await _db.UnifiedCommands.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.ChzzkUid == targetUid);
            
            if (entity != null)
            {
                _db.UnifiedCommands.Remove(entity);
                await _db.SaveChangesAsync();
                await _cacheService.RefreshUnifiedAsync(targetUid, default);
            }

            return Results.Ok();
        }

        /// <summary>
        /// [v1.2] 마스터 데이터 조회 (24시간 인메모리 캐시 적용)
        /// role: All (UI 구성을 위한 공용 데이터)
        /// </summary>
        [HttpGet("/api/commands/master")]
        [AllowAnonymous] 
        public async Task<IResult> GetMasterData()
        {
            var masterData = await _masterCache.GetMasterDataAsync();
            return Results.Ok(masterData);
        }

        /// <summary>
        /// [v1.2] 마스터 데이터 캐시 강제 갱신
        /// role: Admin Only (ChannelManager 정책에 의해 보호됨)
        /// </summary>
        [HttpPost("/api/commands/master/refresh")]
        public IResult RefreshMasterCache()
        {
            _masterCache.RefreshCache();
            _logger.LogInformation("Command Master Cache has been refreshed by administrator.");
            return Results.Ok(new { Message = "Master cache refreshed successfully." });
        }

        // --- 레거시 지원 (하위 호환성 위해 유지하되 내부는 비워둠) ---
        [HttpGet("/api/commands/list/{chzzkUid}")]
        public async Task<IResult> GetCommands(string chzzkUid) => Results.Ok(new List<CombinedCommandDto>());

        [HttpPost("/api/commands/save/{chzzkUid}")]
        public async Task<IResult> SaveCommand(string chzzkUid, [FromBody] StreamerCommand cmd) => Results.Ok();

        [HttpDelete("/api/commands/delete/{chzzkUid}/{idStr}")]
        public async Task<IResult> DeleteCommand(string chzzkUid, string idStr) => Results.Ok();
    }
}
