using MooldangBot.Domain.Contracts.Hubs;

namespace MooldangBot.Domain.Common.Constants
{
    /// <summary>
    /// [오시리스의 전언]: SignalR 통신에서 사용되는 이벤트 명칭을 상수로 정의합니다.
    /// (v15.2: IOverlayClient 인터페이스의 메서드 명칭과 동기화되어 타입 안전성을 보장합니다.)
    /// </summary>
    public static class OverlayEvents
    {
        // nameof 연산자를 사용하여 인터페이스 메서드명과 물리적으로 연결 (참조 정합성 확보)
        public const string ReceiveChat = nameof(IOverlayClient.ReceiveChat);
        public const string ReceiveRouletteResult = nameof(IOverlayClient.ReceiveRouletteResult);
        public const string MissionReceived = nameof(IOverlayClient.MissionReceived);
        public const string ReceiveSongOverlayUpdate = nameof(IOverlayClient.ReceiveSongOverlayUpdate);
        public const string SongAdded = nameof(IOverlayClient.SongAdded);
        public const string NotifySongQueueChanged = nameof(IOverlayClient.NotifySongQueueChanged);
        public const string RefreshSongAndDashboard = nameof(IOverlayClient.RefreshSongAndDashboard);
    }
}
