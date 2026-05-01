using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Infrastructure.Persistence;

namespace MooldangBot.Api.Controllers.Admin;

/// <summary>
/// [함선 관제소 - 명령어]: 관리자가 특정 스트리머의 명령어를 제어합니다.
/// </summary>
[ApiController]
[Route("api/admin/command/{chzzkUid}")]
[Authorize(Roles = "master")]
public class AdminCommandController(IUnifiedCommandService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCommands(string chzzkUid, [FromQuery] CursorPagedRequest request)
    {
        try
        {
            var result = await service.GetPagedCommandsAsync(chzzkUid, request);
            return Ok(Result<CursorPagedResponse<UnifiedCommandDto>>.Success(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<string>.Failure(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCommand(string chzzkUid, int id)
    {
        try
        {
            await service.DeleteCommandAsync(chzzkUid, id);
            return Ok(Result<bool>.Success(true));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<string>.Failure(ex.Message));
        }
    }
}
