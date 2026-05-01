using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Modules.SongBook.Features.Queries;

namespace MooldangBot.Api.Controllers.Admin;

/// <summary>
/// [함선 관제소 - 노래책]: 관리자가 특정 스트리머의 노래책을 제어합니다.
/// </summary>
[ApiController]
[Route("api/admin/songbook/{chzzkUid}")]
[Authorize(Roles = "master")]
public class AdminSongBookController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSongs(string chzzkUid, [FromQuery] string? query)
    {
        var result = await mediator.Send(new GetSongBookLibraryQuery(chzzkUid, query));
        return Ok(result);
    }
}
