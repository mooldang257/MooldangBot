using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MooldangBot.Application.State;

namespace MooldangBot.Presentation.Hubs;

/// <summary>
/// [오시리스의 지휘소]: 서버와 오버레이 간의 실시간 공명 통로입니다.
/// (Aegis of Resonance): 이제 JWT(오버레이)와 Cookie(대시보드) 인증을 모두 지원합니다.
/// </summary>
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
[EnableRateLimiting("overlay-high")]
public class OverlayHub(
    IRouletteService rouletteService, 
    IPulseService pulseService, // [Phase 10] 맥박 서비스 연동
    ILogger<OverlayHub> logger, 
    OverlayState overlayState) : Hub
{
    /// <summary>
    /// [v1.9.9] 오버레이 애니메이션 완료 시 서버에 결과를 알립니다.
    /// </summary>
    public async Task CompleteRouletteAsync(string spinId)
    {
        await rouletteService.CompleteRouletteAsync(spinId);
    }

    // [v2.1.0] OBS 브라우저 소스 클라이언트가 연결될 때 호출 (JWT 클레임 전용)
    public override async Task OnConnectedAsync()
    {
        // 🔐 [오시리스의 절대 인장]: 오직 JWT 토큰 내에 서명된 StreamerId 클레임만 신뢰합니다.
        var chzzkUid = Context.User?.FindFirst("StreamerId")?.Value;

        if (!string.IsNullOrWhiteSpace(chzzkUid))
        {
            var normalizedUid = chzzkUid.ToLower();
            await Groups.AddToGroupAsync(Context.ConnectionId, normalizedUid);
            await overlayState.IncrementAsync(normalizedUid); // [v13.0] Redis 분산 카운트 증가
            logger.LogInformation("[오시리스의 공명] 오버레이 연결 성공. Group: {ChzzkUid}, ConnectionId: {ConnectionId}", normalizedUid, Context.ConnectionId);
        }
        else
        {
            logger.LogWarning("[오시리스의 불협화음] 유효한 클레임 없는 오버레이 연결 시도 차단. ConnectionId: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var chzzkUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrWhiteSpace(chzzkUid))
        {
            await overlayState.DecrementAsync(chzzkUid.ToLower()); // [v13.0] Redis 분산 카운트 감소
        }

        logger.LogTrace("[오시리스의 잔상] 오버레이 연결 종료. Group: {ChzzkUid}, ConnectionId: {ConnectionId}", chzzkUid ?? "Unknown", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// [v2.2.0] 클라이언트 매개변수 의존성을 제거하고 오직 토큰의 클레임만 신뢰합니다.
    /// </summary>
    public async Task JoinStreamerGroup()
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, streamerUid.ToLower());
        }
    }

    public async Task LeaveStreamerGroup()
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, streamerUid.ToLower());
        }
    }

    // 💡 대시보드 상태 업데이트를 동일 그룹(스트리머)의 오버레이들에 전송
    public async Task UpdateOverlayState(string stateJson)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayState", stateJson);
        }
    }

    // 특정 프리셋 그룹에 가입 (프리셋별 독립 업데이트 지원)
    public async Task JoinPresetGroup(int presetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"preset-{presetId}");
    }

    public async Task LeavePresetGroup(int presetId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"preset-{presetId}");
    }

    // 프리셋 레이아웃 업데이트 브로드캐스트
    public async Task UpdatePresetStyle(int presetId, string styleJson)
    {
        await Clients.Group($"preset-{presetId}").SendAsync("ReceiveOverlayStyle", styleJson);
    }

    // 💡 디자인 설정 업데이트를 동일 그룹(스트리머)의 오버레이들에 전송
    public async Task UpdateOverlayStyle(string styleJson)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayStyle", styleJson);
        }
    }

    /// <summary>
    /// [v2.2.1] 오버레이 클라이언트의 실시간 생존 맥박을 수신합니다.
    /// </summary>
    public async Task ReportPulse()
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            pulseService.ReportPulse($"Overlay:{streamerUid.ToLower()}");
        }
    }
}