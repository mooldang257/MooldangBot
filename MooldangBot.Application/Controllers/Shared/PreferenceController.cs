using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Common.Models;
using System.Security.Claims;

namespace MooldangBot.Application.Controllers.Shared;

/// <summary>
/// [?�시리스????: ?�용?�의 개인???�정(Preference)??관리하???�이브리??컨트롤러?�니??
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "ChannelManager")]
// [v10.1] Primary Constructor ?�용 (?�더?�코???�거)
public class PreferenceController(
    IPreferenceCacheService cacheService,
    IPreferenceDbService dbService) : ControllerBase
{
    // --- ?�발???�정 (Temporary / Redis) ---

    /// <summary>
    /// [?�시] ?�정 ?�정값을 Redis?�서 조회?�니??
    /// </summary>
    [HttpGet("/api/Preference/temporary/{chzzkUid}/{key}")]
    public async Task<IActionResult> GetTemporaryPreference(string chzzkUid, string key)
    {
        var sessionUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(sessionUid)) 
            return Unauthorized(Result<string>.Failure("?�증???�요?�니??"));

        var value = await cacheService.GetPreferenceAsync(chzzkUid, key);
        return Ok(Result<object>.Success(new { key, value }));
    }

    /// <summary>
    /// [?�시] ?�정 ?�정값을 Redis???�?�합?�다. (기본 24?�간 TTL)
    /// </summary>
    [HttpPost("/api/Preference/temporary/{chzzkUid}/{key}")]
    public async Task<IActionResult> SetTemporaryPreference(string chzzkUid, string key, [FromBody] PreferenceRequest request)
    {
        var sessionUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(sessionUid)) 
            return Unauthorized(Result<string>.Failure("?�증???�요?�니??"));

        var expiry = request.TtlMinutes.HasValue 
            ? TimeSpan.FromMinutes(request.TtlMinutes.Value) 
            : TimeSpan.FromDays(1);

        await cacheService.SetPreferenceAsync(chzzkUid, key, request.Value, expiry);
        return Ok(Result<bool>.Success(true));
    }

    // --- ?�구 ?�정 (Permanent / MariaDB) ---

    /// <summary>
    /// [?�구] ?�정 ?�정값을 MariaDB?�서 조회?�니??
    /// </summary>
    [HttpGet("/api/Preference/permanent/{key}")]
    public async Task<IActionResult> GetPermanentPreference(string key)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) 
            return Unauthorized(Result<string>.Failure("?�증???�요?�니??"));

        var value = await dbService.GetPermanentPreferenceAsync(chzzkUid, key);
        return Ok(Result<object>.Success(new { key, value }));
    }

    /// <summary>
    /// [?�구] ?�정 ?�정값을 MariaDB???�???�는 ?�데?�트?�니??
    /// </summary>
    [HttpPost("/api/Preference/permanent/{key}")]
    public async Task<IActionResult> SetPermanentPreference(string key, [FromBody] PreferenceRequest request)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) 
            return Unauthorized(Result<string>.Failure("?�증???�요?�니??"));

        await dbService.SetPermanentPreferenceAsync(chzzkUid, key, request.Value);
        return Ok(Result<bool>.Success(true));
    }
}

/// <summary>
/// ?�정 ?�???�청 DTO
/// </summary>
public record PreferenceRequest(string Value, int? TtlMinutes);
