using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Application.Controllers.SongQueue
{
    [ApiController]
    [Route("api/song/{chzzkUid}")]
    [Authorize(Policy = "chzzk-access")]
    public class SongController(ISongQueueService service) : ControllerBase
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

        [HttpPost]
        public async Task<IActionResult> AddSong(string chzzkUid, [FromBody] SongAddRequest request, [FromQuery] int? omakaseId = null)
        {
            var result = await service.AddSongAsync(chzzkUid, request, omakaseId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string chzzkUid, int id, [FromQuery] SongStatus status)
        {
            var result = await service.UpdateStatusAsync(chzzkUid, id, status);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpDelete("bulk")]
        public async Task<IActionResult> DeleteSongs(string chzzkUid, [FromBody] List<int> ids)
        {
            var result = await service.DeleteSongsAsync(chzzkUid, ids);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSongDetails(string chzzkUid, int id, [FromBody] SongUpdateRequest request)
        {
            var result = await service.UpdateSongDetailsAsync(chzzkUid, id, request);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpDelete("clear/{status}")]
        public async Task<IActionResult> ClearSongsByStatus(string chzzkUid, SongStatus status)
        {
            var result = await service.ClearSongsByStatusAsync(chzzkUid, status);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> ReorderSongs(string chzzkUid, [FromBody] List<int> ids)
        {
            var result = await service.ReorderSongsAsync(chzzkUid, ids);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
