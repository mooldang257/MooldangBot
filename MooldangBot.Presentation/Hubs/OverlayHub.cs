using Microsoft.AspNetCore.SignalR;
using MooldangBot.Application.Interfaces; // [v1.9.9] 추가

namespace MooldangBot.Presentation.Hubs;

/// <summary>
/// [오시리스의 지휘소]: 서버와 오버레이 간의 실시간 공명 통로입니다.
/// </summary>
public class OverlayHub(IRouletteService rouletteService) : Hub
{
    /// <summary>
    /// [v1.9.9] 오버레이 애니메이션 완료 시 서버에 결과를 알립니다.
    /// </summary>
    public async Task CompleteRouletteAsync(string spinId)
    {
        await rouletteService.CompleteRouletteAsync(spinId);
    }

    // OBS 브라우저 소스 클라이언트가 연결될 때 호출
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", "Overlay successfully connected.");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// [그룹의 소속]: 스트리머의 UID를 기준으로 오버레이들을 그룹화합니다.
    /// </summary>
    public async Task JoinStreamerGroup(string chzzkUid)
    {
        if (string.IsNullOrEmpty(chzzkUid)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, chzzkUid.ToLower());
    }

    public async Task LeaveStreamerGroup(string chzzkUid)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chzzkUid.ToLower());
    }

    // 💡 대시보드 상태 업데이트를 동일 그룹(chzzkUid)의 오버레이들에 전송
    public async Task UpdateOverlayState(string chzzkUid, string stateJson)
    {
        await Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveOverlayState", stateJson);
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

    // 💡 디자인 설정 업데이트를 동일 그룹(chzzkUid)의 오버레이들에 전송
    public async Task UpdateOverlayStyle(string chzzkUid, string styleJson)
    {
        await Clients.Group(chzzkUid.ToLower()).SendAsync("ReceiveOverlayStyle", styleJson);
    }
}