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
        private readonly IUnifiedCommandService _unifiedCommandService; // [v1.8] 통합 서비스 도입
        private readonly ICommandMasterCacheService _masterCache;
        private readonly ILogger<CommandsController> _logger;

        public CommandsController(
            IAppDbContext db, 
            ICommandCacheService cacheService, 
            IUnifiedCommandService unifiedCommandService, // [v1.8] 주입
            ICommandMasterCacheService masterCache, 
            ILogger<CommandsController> logger)
        {
            _db = db;
            _cacheService = cacheService;
            _unifiedCommandService = unifiedCommandService;
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
            [FromQuery] int pageSize = 1000)
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
        /// [v1.8] 통합 명령어 저장 또는 수정 (서비스 레이어 위임)
        /// </summary>
        [HttpPost("/api/commands/unified/{chzzkUid}")]
        public async Task<IResult> UpsertUnifiedCommand(string chzzkUid, [FromBody] SaveUnifiedCommandRequest req)
        {
            try
            {
                var entity = await _unifiedCommandService.UpsertCommandAsync(chzzkUid, req);
                return Results.Ok(new { Message = req.Id > 0 ? "수정 완료" : "생성 완료", Id = entity.Id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        }

        // [레거시 지원] /save/ 경로도 신규 로직으로 연동
        [HttpPost("/api/commands/unified/save/{chzzkUid}")]
        public async Task<IResult> SaveUnifiedCommand(string chzzkUid, [FromBody] SaveUnifiedCommandRequest req) 
            => await UpsertUnifiedCommand(chzzkUid, req);

        [HttpDelete("/api/commands/unified/delete/{chzzkUid}/{id}")]
        public async Task<IResult> DeleteUnifiedCommand(string chzzkUid, int id)
        {
            await _unifiedCommandService.DeleteCommandAsync(chzzkUid, id);
            return Results.Ok();
        }

        [HttpPatch("/api/commands/unified/toggle/{chzzkUid}/{id}")]
        public async Task<IResult> ToggleUnifiedCommand(string chzzkUid, int id)
        {
            await _unifiedCommandService.ToggleCommandAsync(chzzkUid, id);
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

        // --- Legacy Support ---
        [HttpGet("/api/commands/list/{chzzkUid}")]
        public async Task<IResult> GetCommands(string chzzkUid) => Results.Ok(new List<CombinedCommandDto>());

        [HttpPost("/api/commands/save/{chzzkUid}")]
        public async Task<IResult> SaveCommand(string chzzkUid, [FromBody] object cmd) => Results.Ok();

        [HttpDelete("/api/commands/delete/{chzzkUid}/{idStr}")]
        public async Task<IResult> DeleteCommand(string chzzkUid, string idStr) => Results.Ok();
    }
}
