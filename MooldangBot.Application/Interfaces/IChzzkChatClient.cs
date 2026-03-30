using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [끊기지 않는 소통]: 치지직 WebSocket 채팅 서버와의 실제 통신을 담당하는 저수준 인터페이스입니다.
/// </summary>
public interface IChzzkChatClient : IAsyncDisposable
{
    /// <summary>
    /// [파동의 정렬]: 분산 환경에서 인스턴스 인덱스를 자동으로 할당받고 시스템을 초기화합니다.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// [연결 상태 확인]: 특정 채널의 소켓이 현재 정상 연결(Open) 상태인지 확인합니다.
    /// </summary>
    bool IsConnected(string chzzkUid);

    /// <summary>
    /// [파동의 시작]: 최신 토큰을 사용하여 특정 채널의 채팅 소켓 연결을 시도합니다.
    /// </summary>
    Task<bool> ConnectAsync(string chzzkUid, string accessToken);

    /// <summary>
    /// [파동의 정리]: 특정 채널의 소켓 연결을 안전하게 종료하고 자원을 해제합니다.
    /// </summary>
    Task DisconnectAsync(string chzzkUid);

    /// <summary>
    /// [맥박의 집계]: 현재 연결된 활성 소켓의 총 개수를 반환합니다.
    /// </summary>
    int GetActiveConnectionCount();

    /// <summary>
    /// [파동의 전수조사]: 모든 샤드의 상세 상태 정보를 반환합니다.
    /// </summary>
    IEnumerable<ShardStatus> GetShardStatuses();
}
