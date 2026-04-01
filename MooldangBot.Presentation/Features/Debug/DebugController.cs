using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Workers;
using MooldangBot.Domain.Common;

namespace MooldangBot.Presentation.Features.Debug
{
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly IAppDbContext _db;
        private readonly ChzzkBackgroundService _chzzkService;

        public DebugController(IAppDbContext db, ChzzkBackgroundService chzzkService)
        {
            _db = db;
            _chzzkService = chzzkService;
        }

        [HttpGet("system-check")]
        public async Task<IActionResult> CheckSystem()
        {
            var streamers = await _db.StreamerProfiles
                .Select(p => new { 
                    p.ChannelName, 
                    p.ChzzkUid, 
                    p.IsBotEnabled, 
                    HasToken = !string.IsNullOrEmpty(p.ChzzkAccessToken),
                    TokenExpiry = p.TokenExpiresAt
                })
                .ToListAsync();

            return Ok(new { 
                time = KstClock.Now,
                streamerCount = streamers.Count,
                streamers = streamers
            });
        }
    }
}
