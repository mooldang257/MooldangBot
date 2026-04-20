using MooldangBot.Modules.Commands.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.Commands
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor ?곸슜
    public class CommandsController(
        IAppDbContext db, 
        IUnifiedCommandService unifiedCommandService,
        ICommandMasterCacheService masterCache, 
        ILogger<CommandsController> logger,
        IConfiguration config) : ControllerBase
    {
        /// <summary>
        /// [v10.0] 통합 명령어 목록 조회 (커서 기반 페이지네이션)
        /// </summary>
        [HttpGet("/api/commands/{chzzkUid}")]
        public async Task<IActionResult> GetUnifiedCommands(
            string chzzkUid, 
            [FromQuery] CursorPagedRequest request)
        {
            var streamer = await db.StreamerProfiles
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (streamer == null) 
                return NotFound(Result<string>.Failure("??듃?щ㉧??李얠??????뒿??떎."));
            
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
                    c.RequiredRole.ToString(),
                    c.MatchType.ToString(),
                    c.RequiresSpace,
                    c.Priority
                );
            }).ToList();

            bool hasNext = items.Count > effectiveLimit;
            if (hasNext) items.RemoveAt(effectiveLimit);

            int? nextCursor = items.LastOrDefault()?.Id;

            return Ok(Result<CursorPagedResponse<UnifiedCommandDto>>.Success(new CursorPagedResponse<UnifiedCommandDto>(items, nextCursor, hasNext)));
        }

        /// <summary>
        /// [v10.0] 통합 명령어 생성 및 수정 (Upsert 패턴)
        /// </summary>
        [HttpPost("/api/commands/{chzzkUid}")]
        public async Task<IActionResult> UpsertUnifiedCommand(string chzzkUid, [FromBody] SaveUnifiedCommandRequest req)
        {
            try
            {
                var entity = await unifiedCommandService.UpsertCommandAsync(chzzkUid, req);
                return Ok(Result<object>.Success(new { Message = req.Id > 0 ? "??왂 ?꾨즺" : "??꽦 ?꾨즺", Id = entity.Id }));
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


        [HttpDelete("/api/commands/{chzzkUid}/{id}")]
        public async Task<IActionResult> DeleteUnifiedCommand(string chzzkUid, int id)
        {
            await unifiedCommandService.DeleteCommandAsync(chzzkUid, id);
            return Ok(Result<bool>.Success(true));
        }

        [HttpPatch("/api/commands/{chzzkUid}/{id}/status")]
        public async Task<IActionResult> ToggleUnifiedCommand(string chzzkUid, int id)
        {
            await unifiedCommandService.ToggleCommandAsync(chzzkUid, id);
            return Ok(Result<bool>.Success(true));
        }

        /// <summary>
        /// [v1.2] 留덉????곗씠??議고??(24??�컙 ?몃찓紐⑤??�?��???곸슜)
        /// </summary>
        [HttpGet("/api/commands/master")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetMasterData()
        {
            var masterData = await masterCache.GetMasterDataAsync();
            return Ok(Result<object>.Success(masterData));
        }

        /// <summary>
        /// [v1.2] 留덉????곗씠??�?��??媛뺤??媛깆??
        /// </summary>
        [HttpPost("/api/commands/master/refresh")]
        public IActionResult RefreshMasterCache()
        {
            masterCache.RefreshCache();
            logger.LogInformation("Command Master Cache has been refreshed by administrator.");
            return Ok(Result<object>.Success(new { Message = "Master cache refreshed successfully." }));
        }

    }
}
