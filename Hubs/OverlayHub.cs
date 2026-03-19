using Microsoft.AspNetCore.SignalR;

namespace MooldangAPI.Hubs;

public class OverlayHub : Hub
{
    // OBS 브라우저 소스 클라이언트가 연결될 때 호출
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", "Overlay successfully connected.");
        await base.OnConnectedAsync();
    }

    // 클라이언트가 특정 스트리머의 채널 알림을 구독하도록 그룹에 추가합니다.
    public async Task JoinStreamerGroup(string chzzkUid)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chzzkUid);
    }

    public async Task LeaveStreamerGroup(string chzzkUid)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chzzkUid);
    }
}