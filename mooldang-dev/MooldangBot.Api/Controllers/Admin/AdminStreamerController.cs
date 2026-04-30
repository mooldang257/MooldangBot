using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Entities;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Api.Controllers.Admin;

/// <summary>
/// [함선 관리국]: 모든 스트리머(함선)의 프로필과 상태를 관리하는 컨트롤러입니다.
/// </summary>
[ApiController]
[Route("api/admin/streamers")]
public class AdminStreamerController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStreamers([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var baseQuery = dbContext.CoreStreamerProfiles
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            baseQuery = baseQuery.Where(s => 
                (s.ChannelName != null && s.ChannelName.Contains(query)) || 
                s.ChzzkUid.Contains(query) || 
                (s.Slug != null && s.Slug.Contains(query))
            );
        }

        var totalCount = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new {
                s.Id,
                s.ChzzkUid,
                s.ChannelName,
                s.ProfileImageUrl,
                s.Slug,
                s.IsActive,
                s.CreatedAt,
                s.BotNickname
            })
            .ToListAsync();

        return Ok(Result<object>.Success(new {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStreamer(int id)
    {
        var streamer = await dbContext.CoreStreamerProfiles
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (streamer == null) return NotFound(Result<StreamerProfile>.Failure("존재하지 않는 스트리머입니다."));

        return Ok(Result<StreamerProfile>.Success(streamer));
    }
}
