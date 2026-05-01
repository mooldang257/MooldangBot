using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Domain.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Extensions;

namespace MooldangBot.Application.Controllers.Commands
{
    [ApiController]
    [Route("api/command/{chzzkUid}")]
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
            [FromQuery] CursorPagedRequest request)
        {
            try 
            {
                var result = await unifiedCommandService.GetPagedCommandsAsync(chzzkUid, request);
                return Ok(Result<CursorPagedResponse<UnifiedCommandDto>>.Success(result));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(Result<string>.Failure(ex.Message));
            }
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
        [HttpGet("master")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetMasterData()
        {
            var masterData = await masterCache.GetMasterDataAsync();
            return Ok(Result<object>.Success(masterData));
        }

        /// <summary>
        /// 마스터 데이터 캐시 강제 갱신
        /// </summary>
        [HttpPost("master/refresh")]
        public IActionResult RefreshMasterCache()
        {
            masterCache.RefreshCache();
            logger.LogInformation("Command Master Cache has been refreshed by administrator.");
            return Ok(Result<object>.Success(new { Message = "Master cache refreshed successfully." }));
        }

    }
}
