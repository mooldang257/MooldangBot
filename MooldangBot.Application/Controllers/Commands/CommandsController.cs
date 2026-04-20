using MooldangBot.Modules.Commands.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Domain.Common.Models;
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
    [Route("api/commands/{chzzkUid}")]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor 활용
    public class CommandsController(
        IAppDbContext db, 
        IUnifiedCommandService unifiedCommandService,
        ICommandMasterCacheService masterCache, 
        ILogger<CommandsController> logger,
        IConfiguration config) : ControllerBase
    {
        /// <summary>
        /// 통합 명령어 목록 조회 (커서 기반 페이지네이션)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnifiedCommands(
            string chzzkUid, 
            [FromQuery] PagedRequest request)
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

            if (request.Cursor.HasValue && request.Cursor.Value > 0)
            {
                query = query.Where(c => c.Id < request.Cursor.Value);
            }

            var pagedResult = await query
                .OrderByDescending(c => c.Id)
                .Select(c => new {
                    Entity = c,
                    Meta = CommandFeatureRegistry.GetByType(c.FeatureType)
                })
                .ToPagedListAsync(effectiveLimit, x => x.Entity.Id);

            var items = pagedResult.Items.Select(x => {
                var c = x.Entity;
                return new UnifiedCommandDto(
                    c.Id, 
                    c.Keyword, 
                    x.Meta != null ? ((CommandCategory)(x.Meta.CategoryId - 1)).ToString() : "General", 
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

            return Ok(Result<PagedResponse<UnifiedCommandDto>>.Success(new PagedResponse<UnifiedCommandDto>(items, pagedResult.NextCursor, pagedResult.HasNext)));
        }

        /// <summary>
        /// 통합 명령어 생성 및 수정 (Upsert 패턴)
        /// </summary>
        [HttpPost]
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


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUnifiedCommand(string chzzkUid, int id)
        {
            await unifiedCommandService.DeleteCommandAsync(chzzkUid, id);
            return Ok(Result<bool>.Success(true));
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleUnifiedCommand(string chzzkUid, int id)
        {
            await unifiedCommandService.ToggleCommandAsync(chzzkUid, id);
            return Ok(Result<bool>.Success(true));
        }

        /// <summary>
        /// 마스터 데이터 조회 (24시간 인메모리 캐시 적용)
        /// </summary>
        [HttpGet("/api/commands/master")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetMasterData()
        {
            var masterData = await masterCache.GetMasterDataAsync();
            return Ok(Result<object>.Success(masterData));
        }

        /// <summary>
        /// 마스터 데이터 캐시 강제 갱신
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
