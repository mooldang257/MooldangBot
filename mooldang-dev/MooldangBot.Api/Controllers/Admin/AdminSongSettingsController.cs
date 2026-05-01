using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Modules.SongBook.Features.Queries;
using MooldangBot.Modules.SongBook.Features.Commands;

namespace MooldangBot.Api.Controllers.Admin;

/// <summary>
/// [함선 관제소 - 노래 설정]: 관리자가 특정 스트리머의 노래 설정을 제어합니다.
/// </summary>
[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = "master")]
public class AdminSongSettingsController(IMediator mediator) : ControllerBase
{
    [HttpGet("songlist/{chzzkUid}")]
    public async Task<IActionResult> GetSettings(string chzzkUid)
    {
        var result = await mediator.Send(new GetSonglistSettingsDataQuery(chzzkUid));
        return Ok(result);
    }

    [HttpPost("update/{chzzkUid}")]
    public async Task<IActionResult> UpdateSettings(string chzzkUid, [FromBody] UpdateSonglistSettingsCommand command)
    {
        // [물멍]: 커맨드 객체 내의 UID가 경로와 다를 경우 경로 우선 적용 (보안 강화)
        var result = await mediator.Send(command);
        return Ok(result);
    }
}
