using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Contracts.SongBook;

namespace MooldangBot.Domain.Contracts.Hubs;

/// <summary>
/// [오시리스의 전령]: 오버레이 클라이언트가 수신하는 실시간 메시지 정의
/// (Voice of Resonance): Strongly-typed Hub를 통해 런타임 오타 에러를 방지합니다.
/// </summary>
public interface IOverlayClient
{
    // [v2.4.0]: 기존 문자열 기반 이벤트를 타입 안전한 메서드로 정의
    Task ReceiveOverlayState(string json);
    Task ReceiveOverlayStyle(string json);
    Task ReceiveRouletteResult(object response);
    Task ReceiveSongOverlayUpdate(SongOverlayDto data);
    Task MissionReceived(RouletteMissionOverlayDto missionDto);
    Task ReceiveChat(string json);
    
    // [Legacy/Compat]: 하위 호환성을 위해 유지하거나 필요 시 추가
    Task SongAdded(string sender, string message);
    Task NotifySongQueueChanged();
    Task RefreshSongAndDashboard();
}
