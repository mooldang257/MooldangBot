using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Contracts.Common.Models;

namespace MooldangBot.Application.Controllers.PeriodicMessages
{
    [ApiController]
    [Route("api/PeriodicMessage")]
    [Authorize(Policy = "ChannelManager")]
    // [v10.1] Primary Constructor 적용
    public class PeriodicMessageController(IAppDbContext db) : ControllerBase
    {
        [HttpGet("list/{chzzkUid}")]
        public async Task<IActionResult> GetList(string chzzkUid)
        {
            var list = await db.PeriodicMessages
                .Include(m => m.StreamerProfile)
                .Where(m => m.StreamerProfile!.ChzzkUid == chzzkUid)
                .OrderBy(m => m.Id)
                .Select(m => new PeriodicMessageDto
                {
                    Id = m.Id,
                    IntervalMinutes = m.IntervalMinutes,
                    Message = m.Message,
                    IsEnabled = m.IsEnabled
                })
                .ToListAsync();

            return Ok(Result<ListResponse<PeriodicMessageDto>>.Success(new ListResponse<PeriodicMessageDto>(list, list.Count)));
        }

        [HttpPost("save/{chzzkUid}")]
        public async Task<IActionResult> Save(string chzzkUid, [FromBody] PeriodicMessageSaveRequest req)
        {
            if (req.Id > 0)
            {
                var existing = await db.PeriodicMessages
                    .IgnoreQueryFilters()
                    .Include(m => m.StreamerProfile)
                    .FirstOrDefaultAsync(m => m.Id == req.Id && m.StreamerProfile!.ChzzkUid == chzzkUid);
                    
                if (existing == null)
                    return NotFound(Result<string>.Failure("해당 메시지를 찾을 수 없습니다."));

                existing.IntervalMinutes = req.IntervalMinutes;
                existing.Message = req.Message;
                existing.IsEnabled = req.IsEnabled;
            }
            else
            {
                var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
                if (profile == null) 
                    return NotFound(Result<string>.Failure("스트리머 프로필을 찾을 수 없습니다."));

                db.PeriodicMessages.Add(new PeriodicMessage
                {
                    StreamerProfileId = profile.Id,
                    IntervalMinutes = req.IntervalMinutes,
                    Message = req.Message,
                    IsEnabled = req.IsEnabled
                });
            }

            await db.SaveChangesAsync();
            return Ok(Result<bool>.Success(true));
        }

        [HttpDelete("delete/{chzzkUid}/{id}")]
        public async Task<IActionResult> Delete(string chzzkUid, int id)
        {
            var item = await db.PeriodicMessages
                .IgnoreQueryFilters()
                .Include(m => m.StreamerProfile)
                .FirstOrDefaultAsync(m => m.Id == id && m.StreamerProfile!.ChzzkUid == chzzkUid);
                
            if (item == null)
                return NotFound(Result<string>.Failure("해당 메시지를 찾을 수 없습니다."));

            db.PeriodicMessages.Remove(item);
            await db.SaveChangesAsync();
            
            return Ok(Result<bool>.Success(true));
        }

        [HttpPatch("toggle/{chzzkUid}/{id}")]
        public async Task<IActionResult> Toggle(string chzzkUid, int id)
        {
            var item = await db.PeriodicMessages
                .IgnoreQueryFilters()
                .Include(m => m.StreamerProfile)
                .FirstOrDefaultAsync(m => m.Id == id && m.StreamerProfile!.ChzzkUid == chzzkUid);
                
            if (item == null)
                return NotFound(Result<string>.Failure("해당 메시지를 찾을 수 없습니다."));

            item.IsEnabled = !item.IsEnabled;
            await db.SaveChangesAsync();
            
            return Ok(Result<bool>.Success(true));
        }
    }

    public class PeriodicMessageSaveRequest
    {
        public int Id { get; set; }
        public string ChzzkUid { get; set; } = "";
        public int IntervalMinutes { get; set; }
        public string Message { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
    }
}
