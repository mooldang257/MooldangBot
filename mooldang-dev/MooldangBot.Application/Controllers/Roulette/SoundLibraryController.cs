using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Modules.Roulette.Abstractions;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Controllers.Roulette;

/// <summary>
/// [하모니의 보관함]: 스트리머가 저장한 사운드 자원을 관리하는 컨트롤러입니다.
/// </summary>
[ApiController]
[Route("api/admin/roulette/library")]
[Authorize(Policy = "ChannelManager")]
public class SoundLibraryController(IRouletteDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLibrary()
    {
        var chzzkUid = User.FindFirst("StreamerId")?.Value;
        if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

        var assets = await db.FuncSoundAssets
            .Include(a => a.StreamerProfile)
            .Where(a => a.StreamerProfile!.ChzzkUid == chzzkUid)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.SoundUrl,
                a.AssetType,
                a.CreatedAt
            })
            .AsNoTracking()
            .ToListAsync();

        return Ok(Result<object>.Success(assets));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSound(int id)
    {
        var chzzkUid = User.FindFirst("StreamerId")?.Value;
        if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

        var asset = await db.FuncSoundAssets
            .Include(a => a.StreamerProfile)
            .FirstOrDefaultAsync(a => a.Id == id && a.StreamerProfile!.ChzzkUid == chzzkUid);

        if (asset == null) return NotFound(Result<string>.Failure("사운드를 찾을 수 없습니다."));

        db.FuncSoundAssets.Remove(asset);
        await db.SaveChangesAsync(default);

        return Ok(Result<bool>.Success(true));
    }
}
