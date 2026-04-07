using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MooldangBot.Application.Common.Models;

namespace MooldangBot.Presentation.Features.Commands
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor 적용
    public class CommandsController(
        IAppDbContext db, 
        ICommandCacheService cacheService, 
        IUnifiedCommandService unifiedCommandService,
        ICommandMasterCacheService masterCache, 
        IUserSession userSession,
        ILogger<CommandsController> logger,
        IConfiguration config) : ControllerBase
    {
        /// <summary>
        /// [v2.0] 통합 명령어 목록 조회 (커서 기반 페이지네이션 고도화)
        /// </summary>
        [HttpGet("/api/commands/unified/{chzzkUid}")]
        public async Task<IActionResult> GetUnifiedCommands(
            string chzzkUid, 
            [FromQuery] CursorPagedRequest request)
        {
            var streamer = await db.StreamerProfiles
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (streamer == null) 
                return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));
            
            var streamerId = streamer.Id;
            int maxLimit = config.GetValue<int>("Pagination:MaxLimit", 100);
            int effectiveLimit = Math.Min(request.Limit, maxLimit);

            var query = db.UnifiedCommands
                .AsNoTracking()
                .Where(c => c.StreamerProfileId == streamerId);

            if (request.Cursor.HasValue)
            {
                query = query.Where(c => c.Id < request.Cursor.Value);
            }

            var rawItems = await query
                .OrderByDescending(c => c.Id)
                .Take(effectiveLimit + 1)
                .ToListAsync();

            var items = rawItems.Select(c => {
                var meta = CommandFeatureRegistry.GetByType(c.FeatureType);
                return new UnifiedCommandDto(
                    c.Id, 
                    c.Keyword, 
                    meta != null ? ((CommandCategory)(meta.CategoryId - 1)).ToString() : "General", 
                    c.CostType.ToString(), 
                    c.Cost, 
                    c.FeatureType.ToString(), 
                    c.ResponseText, 
                    c.TargetId, 
                    c.IsActive,
                    c.RequiredRole.ToString()
                );
            }).ToList();

            bool hasNext = items.Count > effectiveLimit;
            if (hasNext) items.RemoveAt(effectiveLimit);

            int? nextCursor = items.LastOrDefault()?.Id;

            return Ok(Result<CursorPagedResponse<UnifiedCommandDto>>.Success(new CursorPagedResponse<UnifiedCommandDto>(items, nextCursor, hasNext)));
        }

        /// <summary>
        /// [v1.8] 통합 명령어 저장 또는 수정 (서비스 레이어 위임)
        /// </summary>
        [HttpPost("/api/commands/unified/{chzzkUid}")]
        public async Task<IActionResult> UpsertUnifiedCommand(string chzzkUid, [FromBody] SaveUnifiedCommandRequest req)
        {
            try
            {
                var entity = await unifiedCommandService.UpsertCommandAsync(chzzkUid, req);
                return Ok(Result<object>.Success(new { Message = req.Id > 0 ? "수정 완료" : "생성 완료", Id = entity.Id }));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(Result<string>.Failure(ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(Result<string>.Failure(ex.Message));
            }
        }

        [HttpPost("/api/commands/unified/save/{chzzkUid}")]
        public async Task<IActionResult> SaveUnifiedCommand(string chzzkUid, [FromBody] SaveUnifiedCommandRequest req) 
            => await UpsertUnifiedCommand(chzzkUid, req);

        [HttpDelete("/api/commands/unified/delete/{chzzkUid}/{id}")]
        public async Task<IActionResult> DeleteUnifiedCommand(string chzzkUid, int id)
        {
            await unifiedCommandService.DeleteCommandAsync(chzzkUid, id);
            return Ok(Result<bool>.Success(true));
        }

        [HttpPatch("/api/commands/unified/toggle/{chzzkUid}/{id}")]
        public async Task<IActionResult> ToggleUnifiedCommand(string chzzkUid, int id)
        {
            await unifiedCommandService.ToggleCommandAsync(chzzkUid, id);
            return Ok(Result<bool>.Success(true));
        }

        /// <summary>
        /// [v1.2] 마스터 데이터 조회 (24시간 인메모리 캐시 적용)
        /// </summary>
        [HttpGet("/api/commands/master")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetMasterData()
        {
            var masterData = await masterCache.GetMasterDataAsync();
            return Ok(Result<object>.Success(masterData));
        }

        /// <summary>
        /// [v1.2] 마스터 데이터 캐시 강제 갱신
        /// </summary>
        [HttpPost("/api/commands/master/refresh")]
        public IActionResult RefreshMasterCache()
        {
            masterCache.RefreshCache();
            logger.LogInformation("Command Master Cache has been refreshed by administrator.");
            return Ok(Result<object>.Success(new { Message = "Master cache refreshed successfully." }));
        }

        // --- Legacy Support ---
        [HttpGet("/api/commands/list/{chzzkUid}")]
        public IActionResult GetCommands(string chzzkUid) => Ok(Result<List<CombinedCommandDto>>.Success(new List<CombinedCommandDto>()));

        [HttpPost("/api/commands/save/{chzzkUid}")]
        public IActionResult SaveCommand(string chzzkUid, [FromBody] object cmd) => Ok(Result<bool>.Success(true));

        [HttpDelete("/api/commands/delete/{chzzkUid}/{idStr}")]
        public IActionResult DeleteCommand(string chzzkUid, string idStr) => Ok(Result<bool>.Success(true));
    }
}
