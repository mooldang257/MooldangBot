using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Extensions;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.SysPeriodicMessages
{
    [ApiController]
    [Route("api/periodic-message/{chzzkUid}")]
    [Authorize(Policy = "ChannelManager")]
    public class PeriodicMessageController(IPeriodicMessageService service) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetList(string chzzkUid, [FromQuery] CursorPagedRequest request)
        {
            var pagedResult = await service.GetListAsync(chzzkUid, request);
            return Ok(Result<CursorPagedResponse<PeriodicMessageDto>>.Success(pagedResult));
        }

        [HttpPost]
        public async Task<IActionResult> Save(string chzzkUid, [FromBody] PeriodicMessageSaveRequest req)
        {
            var result = await service.SaveAsync(chzzkUid, req);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string chzzkUid, int id)
        {
            var result = await service.DeleteAsync(chzzkUid, id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> Toggle(string chzzkUid, int id)
        {
            var result = await service.ToggleAsync(chzzkUid, id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }
    }
}
