using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace MooldangBot.Presentation.Hubs;

/// <summary>
/// [오시리스의 지휘소]: 서버와 오버레이 간의 실시간 공명 통로입니다.
/// (Aegis of Resonance): 이제 JWT 토큰(OverlayAuth 정책) 없이는 접근할 수 없습니다.
/// </summary>
[Authorize(Policy = "OverlayAuth")]
public class OverlayHub(IRouletteService rouletteService, ILogger<OverlayHub> logger) : Hub
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
        // ⚠️ [보안 리뷰 반영]: 클라이언트가 조작 가능한 쿼리 스트링 폴백을 제거하여 Group Spoofing 방어
        var chzzkUid = Context.User?.FindFirst("StreamerId")?.Value;

        if (!string.IsNullOrWhiteSpace(chzzkUid))
        {
            // 1. 소문자로 정규화하여 그룹 가입 (대소문자 불일치 방지)
            var normalizedUid = chzzkUid.ToLower();
            await Groups.AddToGroupAsync(Context.ConnectionId, normalizedUid);
            
            // 2. 구조화된 로깅으로 접속 및 자동 가입 기록
            logger.LogInformation("[오시리스의 공명] 오버레이가 성공적으로 연결되었습니다. Group: {ChzzkUid}, ConnectionId: {ConnectionId}", normalizedUid, Context.ConnectionId);
        }
        else
        {
            // 🛑 [오시리스의 불협화음]: 유효한 스트리머 클레임이 없는 경우 연결을 즉시 차단
            logger.LogWarning("[오시리스의 불협화음] 유효한 스트리머 클레임이 없는 오버레이 연결 시도. ConnectionId: {ConnectionId}", Context.ConnectionId);
            Context.Abort(); // 💡 불법 연결 즉시 강제 종료
            return;
        }
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// [그룹의 소속]: 스트리머의 UID를 기준으로 오버레이들을 그룹화합니다.
    /// (Strict Claim Trust): 클라이언트가 전달한 UID 대신 토큰 내의 StreamerId를 사용합니다.
    /// </summary>
    public async Task JoinStreamerGroup(string? chzzkUid = null)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (string.IsNullOrEmpty(streamerUid)) return;
        
        await Groups.AddToGroupAsync(Context.ConnectionId, streamerUid.ToLower());
    }

    public async Task LeaveStreamerGroup(string? chzzkUid = null)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (string.IsNullOrEmpty(streamerUid)) return;
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, streamerUid.ToLower());
    }

    // 💡 대시보드 상태 업데이트를 동일 그룹(스트리머)의 오버레이들에 전송
    public async Task UpdateOverlayState(string stateJson, string? chzzkUid = null)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (string.IsNullOrEmpty(streamerUid)) return;

        await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayState", stateJson);
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
    public async Task UpdateOverlayStyle(string styleJson, string? chzzkUid = null)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (string.IsNullOrEmpty(streamerUid)) return;

        await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayStyle", styleJson);
    }
}