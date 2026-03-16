using Microsoft.AspNetCore.SignalR;

namespace MooldangAPI.Hubs
{
    public class OverlayHub : Hub
    {
        // 1. 오버레이가 켜질 때 특정 스트리머의 채널(방)에 입장
        public async Task JoinStreamerGroup(string chzzkUid)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chzzkUid);
        }

        // 2. 대시보드 -> 오버레이 (곡 목록 및 상태 실시간 전송)
        public async Task UpdateOverlayState(string chzzkUid, string jsonState)
        {
            await Clients.Group(chzzkUid).SendAsync("ReceiveOverlayState", jsonState);
        }

        // ⭐ 3. 설정창 -> 오버레이 (디자인 및 색상 설정 실시간 전송) - 이 부분이 핵심입니다!
        public async Task UpdateOverlayStyle(string chzzkUid, string jsonStyle)
        {
            await Clients.Group(chzzkUid).SendAsync("ReceiveOverlayStyle", jsonStyle);
        }
    }
}