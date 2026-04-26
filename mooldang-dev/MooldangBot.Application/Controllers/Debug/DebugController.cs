using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.Debug
{
    [ApiController]
    [Route("api/debug")]
    // [v10.1] Primary Constructor ?곸슜 - 誘몄궗???뚯빱 ?섏〈???쒓굅
    public class DebugController(IAppDbContext db) : ControllerBase
    {
        [HttpGet("system-check")]
        public async Task<IActionResult> CheckSystem()
        {
            var streamers = await db.CoreStreamerProfiles
                .IgnoreQueryFilters()
                .Select(p => new { 
                    p.ChannelName, 
                    p.ChzzkUid, 
                    p.IsActive, 
                    p.IsMasterEnabled,
                    HasToken = !string.IsNullOrEmpty(p.ChzzkAccessToken),
                    TokenExpiry = p.TokenExpiresAt
                })
                .ToListAsync();

            return Ok(Result<object>.Success(new { 
                time = KstClock.Now,
                streamerCount = streamers.Count,
                streamers = streamers
            }));
        }
    }
}
