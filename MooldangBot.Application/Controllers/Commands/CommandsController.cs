using MooldangBot.Modules.Commands.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Contracts.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MooldangBot.Contracts.Common.Models;

namespace MooldangBot.Application.Controllers.Commands
{
    [ApiController]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor ?Í≥∏Ïäú
    public class CommandsController(
        IAppDbContext db, 
        IUnifiedCommandService unifiedCommandService,
        ICommandMasterCacheService masterCache, 
        ILogger<CommandsController> logger,
        IConfiguration config) : ControllerBase
    {
        /// <summary>
        /// [v2.0] ???? ÔßèÎÇÖÏ°??Ôßè‚ë∏Ï§?Ë≠∞Í≥†??(?å„ÖºÍΩ?Êπ≤Í≥ïÏª???èÏî†Ôßû¬Ä??ºÏî†???®Ï¢äÎ£??
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
                return NotFound(Result<string>.Failure("??ΩÎìÉ?±—â„âß??Ôß°Ïñ†??????ÅÎíø??àÎñé."));
            
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
        /// [v1.8] ???? ÔßèÎÇÖÏ°???????Î®?íó ??èÏ†ô (??ïÌâ¨????âÏî†???Íæ©Ïó´)
        /// </summary>
        [HttpPost("/api/commands/unified/{chzzkUid}")]
        public async Task<IActionResult> UpsertUnifiedCommand(string chzzkUid, [FromBody] SaveUnifiedCommandRequest req)
        {
            try
            {
                var entity = await unifiedCommandService.UpsertCommandAsync(chzzkUid, req);
                return Ok(Result<object>.Success(new { Message = req.Id > 0 ? "??èÏ†ô ?Íæ®Ï¶∫" : "??πÍΩ¶ ?Íæ®Ï¶∫", Id = entity.Id }));
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
        /// [v1.2] ÔßçÎçâ????Í≥óÏî†??Ë≠∞Í≥†??(24??ìÏªô ?Î™ÉÏ∞ìÔßè‚ë§??Ôß?®Ø???Í≥∏Ïäú)
        /// </summary>
        [HttpGet("/api/commands/master")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetMasterData()
        {
            var masterData = await masterCache.GetMasterDataAsync();
            return Ok(Result<object>.Success(masterData));
        }

        /// <summary>
        /// [v1.2] ÔßçÎçâ????Í≥óÏî†??Ôß?®Ø??Â™õÎ∫§??Â™õÍπÜ??
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
