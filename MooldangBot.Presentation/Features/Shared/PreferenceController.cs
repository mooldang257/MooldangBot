using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // IResult, Results 필요
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Common.Models; // Result<T> 도입
using System.Security.Claims;

namespace MooldangBot.Presentation.Features.Shared;

/// <summary>
/// [오시리스의 눈]: 사용자의 개인화 설정(Preference)을 관리하는 하이브리드 컨트롤러입니다.
/// Redis(휘발성)와 MariaDB(영구성) 두 가지 저장소를 전략적으로 활용합니다.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "ChannelManager")]
public class PreferenceController(
    IPreferenceCacheService _cacheService,
    IPreferenceDbService _dbService) : ControllerBase
{
    // --- 휘발성 설정 (Temporary / Redis) ---

    /// <summary>
    /// [임시] 특정 설정값을 Redis에서 조회합니다.
    /// </summary>
    [HttpGet("/api/Preference/temporary/{key}")]
    public async Task<IResult> GetTemporaryPreference(string key)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) return Results.Unauthorized();

        var value = await _cacheService.GetPreferenceAsync(chzzkUid, key);
        return Results.Ok(Result<object>.Success(new { key, value }));
    }

    /// <summary>
    /// [임시] 특정 설정값을 Redis에 저장합니다. (기본 24시간 TTL)
    /// </summary>
    [HttpPost("/api/Preference/temporary/{key}")]
    public async Task<IResult> SetTemporaryPreference(string key, [FromBody] PreferenceRequest request)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) return Results.Unauthorized();

        var expiry = request.TtlMinutes.HasValue 
            ? TimeSpan.FromMinutes(request.TtlMinutes.Value) 
            : TimeSpan.FromDays(1);

        await _cacheService.SetPreferenceAsync(chzzkUid, key, request.Value, expiry);
        return Results.Ok(Result<bool>.Success(true));
    }

    // --- 영구 설정 (Permanent / MariaDB) ---

    /// <summary>
    /// [영구] 특정 설정값을 MariaDB에서 조회합니다.
    /// </summary>
    [HttpGet("/api/Preference/permanent/{key}")]
    public async Task<IResult> GetPermanentPreference(string key)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) return Results.Unauthorized();

        var value = await _dbService.GetPermanentPreferenceAsync(chzzkUid, key);
        return Results.Ok(Result<object>.Success(new { key, value }));
    }

    /// <summary>
    /// [영구] 특정 설정값을 MariaDB에 저장 또는 업데이트합니다.
    /// </summary>
    [HttpPost("/api/Preference/permanent/{key}")]
    public async Task<IResult> SetPermanentPreference(string key, [FromBody] PreferenceRequest request)
    {
        var chzzkUid = User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(chzzkUid)) return Results.Unauthorized();

        await _dbService.SetPermanentPreferenceAsync(chzzkUid, key, request.Value);
        return Results.Ok(Result<bool>.Success(true));
    }
}

/// <summary>
/// 설정 저장 요청 DTO
/// </summary>
public record PreferenceRequest(string Value, int? TtlMinutes);
