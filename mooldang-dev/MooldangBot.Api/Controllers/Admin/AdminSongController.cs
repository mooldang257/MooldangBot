using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Api.Controllers.Admin;

/// <summary>
/// [함선 관제소 - 신청곡]: 관리자가 특정 스트리머의 신청곡 대기열을 제어합니다.
/// </summary>
[ApiController]
[Route("api/admin/song/{chzzkUid}")]
[Authorize(Roles = "master")]
public class AdminSongController(ISongQueueService service) : ControllerBase
{
    [HttpGet("queue")]
    public async Task<IActionResult> GetSongQueue(
        string chzzkUid, 
        [FromQuery] SongStatus? status,
        [FromQuery] CursorPagedRequest request)
    {
        try
        {
            var result = await service.GetPagedQueueAsync(chzzkUid, status, request);
            return Ok(Result<CursorPagedResponse<SongQueueViewDto>>.Success(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<string>.Failure(ex.Message));
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string chzzkUid, int id, [FromQuery] SongStatus status)
    {
        var result = await service.UpdateStatusAsync(chzzkUid, id, status);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSong(string chzzkUid, int id)
    {
        var result = await service.DeleteSongsAsync(chzzkUid, new List<int> { id });
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }
}
