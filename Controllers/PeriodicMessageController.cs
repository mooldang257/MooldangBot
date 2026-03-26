using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ChannelManager")] // 🛡️ 정기 메세지 관리에 채널 매니저 정책 적용
    public class PeriodicMessageController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PeriodicMessageController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("list/{chzzkUid}")]
        public async Task<IActionResult> GetList(string chzzkUid)
        {
            var list = await _db.PeriodicMessages
                .IgnoreQueryFilters() // 💡 [마스터 대응] 필터 우회
                .Where(m => m.ChzzkUid == chzzkUid)
                .OrderBy(m => m.Id)
                .Select(m => new PeriodicMessageDto
                {
                    Id = m.Id,
                    IntervalMinutes = m.IntervalMinutes,
                    Message = m.Message,
                    IsEnabled = m.IsEnabled
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("save/{chzzkUid}")]
        public async Task<IActionResult> Save(string chzzkUid, [FromBody] PeriodicMessageSaveRequest req)
        {
            if (req.Id > 0)
            {
                var existing = await _db.PeriodicMessages
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(m => m.Id == req.Id && m.ChzzkUid == chzzkUid);
                    
                if (existing != null)
                {
                    existing.IntervalMinutes = req.IntervalMinutes;
                    existing.Message = req.Message;
                    existing.IsEnabled = req.IsEnabled;
                }
            }
            else
            {
                _db.PeriodicMessages.Add(new PeriodicMessage
                {
                    ChzzkUid = chzzkUid, // 🛡️ 경로상의 UID로 강제 고정
                    IntervalMinutes = req.IntervalMinutes,
                    Message = req.Message,
                    IsEnabled = req.IsEnabled
                });
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("delete/{chzzkUid}/{id}")]
        public async Task<IActionResult> Delete(string chzzkUid, int id)
        {
            var item = await _db.PeriodicMessages
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == id && m.ChzzkUid == chzzkUid);
                
            if (item != null)
            {
                _db.PeriodicMessages.Remove(item);
                await _db.SaveChangesAsync();
            }
            return Ok();
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
