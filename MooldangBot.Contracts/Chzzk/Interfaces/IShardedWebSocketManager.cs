using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MooldangBot.Contracts.Chzzk.Models;
using MooldangBot.Contracts.Chzzk.Models.Events;

namespace MooldangBot.Contracts.Chzzk.Interfaces;

/// <summary>
/// [오시리스의 지혜군]: 여러 개의 WebSocket 샤드를 총괄 관리하기 위한 인터페이스입니다.
/// </summary>
public interface IShardedWebSocketManager
{
    // 1. 생명주기 관리
    /// <summary>
    /// 샤드 매니저를 초기화하고 지정된 개수의 샤드를 시작합니다.
    /// </summary>
    Task StartAsync(int initialShardCount = 1);

    /// <summary>
    /// 모든 샤드를 안전하게 종료합니다.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    // 2. 연결 관리
    /// <summary>
    /// 특정 채널(ChzzkUid)에 대한 WebSocket 연결을 수행합니다.
    /// </summary>
    Task ConnectAsync(string chzzkUid, string url, string accessToken);

    /// <summary>
    /// 특정 채널에 대한 연결을 해제합니다.
    /// </summary>
    Task DisconnectAsync(string chzzkUid);

    // 3. 상태 모니터링
    /// <summary>
    /// 특정 채널(ChzzkUid)이 현재 WebSocket에 정상적으로 연결되어 있는지 확인합니다.
    /// </summary>
    bool IsConnected(string chzzkUid); // 👈 [추가된 코드]

    /// <summary>
    /// 현재 활성화된 모든 소켓 연결 수를 반환합니다.
    /// </summary>
    int GetActiveConnectionCount();

    /// <summary>
    /// 모든 샤드의 상세 상태 목록을 반환합니다.
    /// </summary>
    Task<IEnumerable<ShardStatus>> GetShardStatusesAsync();

    // 4. 제어 명령 (치지직 API 연동)
    Task<bool> SendMessageAsync(string chzzkUid, string message);
    Task<bool> SendNoticeAsync(string chzzkUid, string message);
    Task<bool> UpdateTitleAsync(string chzzkUid, string newTitle);
    Task<bool> UpdateCategoryAsync(string chzzkUid, string category);

    // 5. 시뮬레이션 및 테스트
    /// <summary>
    /// [v3.6] 외부 시뮬레이터로부터 치지직 원본 JSON 이벤트를 주입받아 처리합니다.
    /// </summary>
    Task<bool> InjectEventAsync(string chzzkUid, string eventName, string rawJson);
}
