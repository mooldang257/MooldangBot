using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Configuration; // [Phase 1] 설정 연동
using MooldangBot.Application.Common.Models; // Result<T> 도입

namespace MooldangBot.Presentation.Features.Commands
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")] // 🛡️ 모든 명령어 관리에 채널 매니저 정책 적용
    public class CommandsController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly ICommandCacheService _cacheService;
        private readonly IUnifiedCommandService _unifiedCommandService;
        private readonly ICommandMasterCacheService _masterCache;
        private readonly IUserSession _userSession; // [v6.1] 세션 정보 주입
        private readonly ILogger<CommandsController> _logger;
        private readonly IConfiguration _config; // [Phase 1] MaxLimit 정책용

        public CommandsController(
            IAppDbContext db, 
            ICommandCacheService cacheService, 
            IUnifiedCommandService unifiedCommandService,
            ICommandMasterCacheService masterCache, 
            IUserSession userSession,
            ILogger<CommandsController> logger,
            IConfiguration config)
        {
            _db = db;
            _cacheService = cacheService;
            _unifiedCommandService = unifiedCommandService;
            _masterCache = masterCache;
            _userSession = userSession;
            _logger = logger;
            _config = config;
        }


        /// <summary>
        /// [v2.0] 통합 명령어 목록 조회 (커서 기반 페이지네이션 고도화)
        /// </summary>
        [HttpGet("/api/commands/unified/{chzzkUid}")]
        public async Task<IResult> GetUnifiedCommands(
            string chzzkUid, 
            [FromQuery] CursorPagedRequest request)
        {
            // 🛡️ 보안: 세션 기반 권한 검증 및 정규화된 ID 조회
            var streamer = await _db.StreamerProfiles
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (streamer == null) return Results.NotFound(Result<object>.Failure("스트리머를 찾을 수 없습니다."));
            
            var streamerId = streamer.Id;
            int maxLimit = _config.GetValue<int>("Pagination:MaxLimit", 100);
            int effectiveLimit = Math.Min(request.Limit, maxLimit);

            // 🔍 기본 쿼리 빌드 (ISoftDeletable 필터 적용을 위해 IgnoreQueryFilters 제외)
            var query = _db.UnifiedCommands
                .AsNoTracking()
                .Include(c => c.MasterFeature)
                .ThenInclude(f => f.Category)
                .Where(c => c.StreamerProfileId == streamerId);

            // 🚀 커서 기반 필터링 (ID 역순/최신순 기준)
            if (request.Cursor.HasValue)
            {
                query = query.Where(c => c.Id < request.Cursor.Value);
            }

            // [Limit + 1] 개를 조회하여 다음 페이지 존재 여부 확인 (IX_UnifiedCommand_CursorPaging 활용)
            var items = await query
                .OrderByDescending(c => c.Id)
                .Take(effectiveLimit + 1)
                .Select(c => new UnifiedCommandDto(
                    c.Id, 
                    c.Keyword, 
                    c.MasterFeature != null && c.MasterFeature.Category != null ? c.MasterFeature.Category.Name : "General", 
                    c.CostType.ToString(), 
                    c.Cost, 
                    c.MasterFeature != null ? c.MasterFeature.TypeName : "Unknown", 
                    c.ResponseText, 
                    c.TargetId, 
                    c.IsActive,
                    c.RequiredRole.ToString()))
                .ToListAsync();

            // 응답 데이터 가공
            bool hasNext = items.Count > effectiveLimit;
            if (hasNext) items.RemoveAt(effectiveLimit);

            int? nextCursor = items.LastOrDefault()?.Id;

            return Results.Ok(Result<CursorPagedResponse<UnifiedCommandDto>>.Success(new CursorPagedResponse<UnifiedCommandDto>(items, nextCursor, hasNext)));
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
                return Results.Ok(Result<object>.Success(new { Message = req.Id > 0 ? "수정 완료" : "생성 완료", Id = entity.Id }));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(Result<object>.Failure(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(Result<object>.Failure(ex.Message));
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
            return Results.Ok(Result<bool>.Success(true));
        }

        [HttpPatch("/api/commands/unified/toggle/{chzzkUid}/{id}")]
        public async Task<IResult> ToggleUnifiedCommand(string chzzkUid, int id)
        {
            await _unifiedCommandService.ToggleCommandAsync(chzzkUid, id);
            return Results.Ok(Result<bool>.Success(true));
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
            return Results.Ok(Result<object>.Success(masterData));
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
            return Results.Ok(Result<object>.Success(new { Message = "Master cache refreshed successfully." }));
        }

        // --- Legacy Support ---
        [HttpGet("/api/commands/list/{chzzkUid}")]
        public async Task<IResult> GetCommands(string chzzkUid) => Results.Ok(Result<List<CombinedCommandDto>>.Success(new List<CombinedCommandDto>()));

        [HttpPost("/api/commands/save/{chzzkUid}")]
        public async Task<IResult> SaveCommand(string chzzkUid, [FromBody] object cmd) => Results.Ok(Result<bool>.Success(true));

        [HttpDelete("/api/commands/delete/{chzzkUid}/{idStr}")]
        public async Task<IResult> DeleteCommand(string chzzkUid, string idStr) => Results.Ok(Result<bool>.Success(true));
    }
}
