using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Application.Common.Models;
using System.Security.Claims;

namespace MooldangBot.Presentation.Features.Shared;

/// <summary>
/// [오시리스의 눈]: 사용자의 개인화 설정(Preference)을 관리하는 하이브리드 컨트롤러입니다.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "ChannelManager")]
// [v10.1] Primary Constructor 적용 (언더스코어 제거)
public class PreferenceController(
    IPreferenceCacheService cacheService,
    IPreferenceDbService dbService) : ControllerBase
{
    // --- 휘발성 설정 (Temporary / Redis) ---

    /// <summary>
    /// [임시] 특정 설정값을 Redis에서 조회합니다.
    /// </summary>
    [HttpGet("/api/Preference/temporary/{key}")]
    public async Task<IActionResult> GetTemporaryPreference(string key)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) 
            return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

        var value = await cacheService.GetPreferenceAsync(chzzkUid, key);
        return Ok(Result<object>.Success(new { key, value }));
    }

    /// <summary>
    /// [임시] 특정 설정값을 Redis에 저장합니다. (기본 24시간 TTL)
    /// </summary>
    [HttpPost("/api/Preference/temporary/{key}")]
    public async Task<IActionResult> SetTemporaryPreference(string key, [FromBody] PreferenceRequest request)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) 
            return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

        var expiry = request.TtlMinutes.HasValue 
            ? TimeSpan.FromMinutes(request.TtlMinutes.Value) 
            : TimeSpan.FromDays(1);

        await cacheService.SetPreferenceAsync(chzzkUid, key, request.Value, expiry);
        return Ok(Result<bool>.Success(true));
    }

    // --- 영구 설정 (Permanent / MariaDB) ---

    /// <summary>
    /// [영구] 특정 설정값을 MariaDB에서 조회합니다.
    /// </summary>
    [HttpGet("/api/Preference/permanent/{key}")]
    public async Task<IActionResult> GetPermanentPreference(string key)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) 
            return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

        var value = await dbService.GetPermanentPreferenceAsync(chzzkUid, key);
        return Ok(Result<object>.Success(new { key, value }));
    }

    /// <summary>
    /// [영구] 특정 설정값을 MariaDB에 저장 또는 업데이트합니다.
    /// </summary>
    [HttpPost("/api/Preference/permanent/{key}")]
    public async Task<IActionResult> SetPermanentPreference(string key, [FromBody] PreferenceRequest request)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) 
            return Unauthorized(Result<string>.Failure("인증이 필요합니다."));

        await dbService.SetPermanentPreferenceAsync(chzzkUid, key, request.Value);
        return Ok(Result<bool>.Success(true));
    }
}

/// <summary>
/// 설정 저장 요청 DTO
/// </summary>
public record PreferenceRequest(string Value, int? TtlMinutes);
