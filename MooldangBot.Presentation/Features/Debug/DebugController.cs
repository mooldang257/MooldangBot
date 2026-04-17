using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Common;
using MooldangBot.Contracts.Common.Models;

namespace MooldangBot.Presentation.Features.Debug
{
    [ApiController]
    [Route("api/debug")]
    // [v10.1] Primary Constructor 적용 - 미사용 워커 의존성 제거
    public class DebugController(IAppDbContext db) : ControllerBase
    {
        [HttpGet("system-check")]
        public async Task<IActionResult> CheckSystem()
        {
            var streamers = await db.StreamerProfiles
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
