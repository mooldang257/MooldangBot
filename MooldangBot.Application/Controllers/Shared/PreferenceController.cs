using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Entities;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Controllers.Shared;

/// <summary>
/// [오시리스의 지혜]: 사용자의 개인적 설정(Preference)을 관리하는 하이브리드 컨트롤러입니다.
/// </summary>
[ApiController]
[Route("api/preference")]
[Authorize]
// [v10.1] Primary Constructor 적용
public class PreferenceController(
    IPreferenceCacheService cacheService,
    IPreferenceDbService dbService,
    IAppDbContext db) : ControllerBase
{
    // --- 휘발성 설정 (Temporary / Redis) ---

    /// <summary>
    /// [임시] 현재 로그인한 스트리머의 설정을 Redis에서 조회합니다.
    /// </summary>
    [HttpGet("temporary/{key}")]
    public async Task<IActionResult> GetMyTemporaryPreference(string key)
    {
        var sessionUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(sessionUid)) 
            return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

        var value = await cacheService.GetPreferenceAsync(sessionUid, key);
        return Ok(Result<object>.Success(new { key, value }));
    }

    /// <summary>
    /// [임시] 특정 스트리머의 설정을 Redis에서 조회합니다. (ID 또는 슬러그 지원)
    /// </summary>
    [HttpGet("temporary/{chzzkUid}/{key}")]
    [Authorize(Policy = "ChannelManager")]
    public async Task<IActionResult> GetTemporaryPreference(string chzzkUid, string key)
    {
        var profile = await GetProfileByUidOrSlugAsync(chzzkUid);
        if (profile == null) return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

        var value = await cacheService.GetPreferenceAsync(profile.ChzzkUid, key);
        return Ok(Result<object>.Success(new { key, value }));
    }

    /// <summary>
    /// [임시] 설정을 Redis에 저장합니다. (기본 24시간 TTL)
    /// </summary>
    [HttpPost("temporary/{key}")]
    public async Task<IActionResult> SetMyTemporaryPreference(string key, [FromBody] PreferenceRequest request)
    {
        var sessionUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(sessionUid)) 
            return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

        var expiry = request.TtlMinutes.HasValue 
            ? TimeSpan.FromMinutes(request.TtlMinutes.Value) 
            : TimeSpan.FromDays(1);

        await cacheService.SetPreferenceAsync(sessionUid, key, request.Value, expiry);
        return Ok(Result<bool>.Success(true));
    }

    // --- 영구 설정 (Permanent / MariaDB) ---

    /// <summary>
    /// [영구] 현재 로그인한 스트리머의 설정을 MariaDB에서 조회합니다.
    /// </summary>
    [HttpGet("permanent/{key}")]
    public async Task<IActionResult> GetPermanentPreference(string key)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) 
            return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

        var value = await dbService.GetPermanentPreferenceAsync(chzzkUid, key);
        return Ok(Result<object>.Success(new { key, value }));
    }

    /// <summary>
    /// [영구] 설정을 MariaDB에 저장하거나 업데이트합니다.
    /// </summary>
    [HttpPost("permanent/{key}")]
    public async Task<IActionResult> SetPermanentPreference(string key, [FromBody] PreferenceRequest request)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) 
            return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

        await dbService.SetPermanentPreferenceAsync(chzzkUid, key, request.Value);
        return Ok(Result<bool>.Success(true));
    }

    private async Task<StreamerProfile?> GetProfileByUidOrSlugAsync(string uid)
    {
        var target = uid.ToLower();
        return await db.StreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == target || (p.Slug != null && p.Slug.ToLower() == target));
    }
}

/// <summary>
/// 설정 저장 요청 DTO
/// </summary>
public record PreferenceRequest(string Value, int? TtlMinutes);
