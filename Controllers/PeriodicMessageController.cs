using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] PeriodicMessageSaveRequest req)
        {
            if (string.IsNullOrEmpty(req.ChzzkUid)) return BadRequest();

            if (req.Id > 0)
            {
                var existing = await _db.PeriodicMessages.FindAsync(req.Id);
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
                    ChzzkUid = req.ChzzkUid,
                    IntervalMinutes = req.IntervalMinutes,
                    Message = req.Message,
                    IsEnabled = req.IsEnabled
                });
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.PeriodicMessages.FindAsync(id);
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
