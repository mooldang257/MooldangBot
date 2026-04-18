using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.DTOs;
using System.Security.Claims;

namespace MooldangBot.Application.Controllers.Overlay;

/// <summary>
/// [오시리스의 열쇠]: 오버레이 전용 JWT를 관리하는 컨트롤러입니다.
/// </summary>
[ApiController]
[Route("api/overlay/auth")]
[Authorize] // 기본적으로 쿠키 인증이 된 상태(대시보드)에서 호출됩니다.
public class OverlayAuthController(IAuthService _authService) : ControllerBase
{
    /// <summary>
    /// [v2.0.0] 현재 로그인한 스트리머의 오버레이용 장기 수명 토큰을 발급합니다.
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> GenerateToken()
    {
        // 1. 현재 로그인한 사용자의 ChzzkUid 추출 (쿠키 세션 기반)
        var chzzkUid = User.FindFirst("ChzzkUid")?.Value;
        if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized("ChzzkUid를 찾을 수 없습니다.");

        try
        {
            // 2. JWT 발급 (Streamer 역할 부여)
            var token = await _authService.IssueOverlayTokenAsync(chzzkUid, "Streamer");
            
            return Ok(new { 
                success = true, 
                token = token,
                message = "오시리스의 공명 토큰이 성공적으로 발급되었습니다. 방송 소스에 이 토큰을 사용하세요."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// [v2.0.0] 기존에 발급된 모든 오버레이 토큰을 즉시 무효화합니다. (버전 업그레이드)
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeTokens()
    {
        var chzzkUid = User.FindFirst("ChzzkUid")?.Value;
        if (string.IsNullOrEmpty(chzzkUid)) return Unauthorized();

        var result = await _authService.RevokeOverlayTokenAsync(chzzkUid);
        
        if (result)
        {
            return Ok(new { 
                success = true, 
                message = "오시리스의 철퇴가 가동되었습니다. 기존의 모든 오버레이 토큰이 즉시 폐기되었습니다. 새로운 토큰을 발급받으세요." 
            });
        }
        
        return BadRequest(new { success = false, message = "토큰 폐기에 실패했습니다." });
    }
}
