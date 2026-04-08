using MooldangBot.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

public interface IChzzkBotService
{
    /// <summary>
    /// 설정된 계정(봇/스트리머)으로 채팅 메시지를 전송합니다. (v1.9 동적 엔진 연동을 위해 viewerUid 추가)
    /// </summary>
    Task<bool> SendReplyChatAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token);
    
    /// <summary>
    /// 설정된 계정으로 상단 공지(Pinned Notice)를 등록합니다. (v1.9.9)
    /// </summary>
    Task<bool> SendReplyNoticeAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token);

    /// <summary>
    /// 방송 제목을 변경합니다. (v2.5)
    /// </summary>
    Task<bool> UpdateTitleAsync(StreamerProfile profile, string newTitle, string senderUid, CancellationToken token);

    /// <summary>
    /// 방송 카테고리를 변경합니다. (v2.6 식별자 기반 정밀 대응 추가)
    /// </summary>
    Task<bool> UpdateCategoryAsync(StreamerProfile profile, string category, string senderUid, string? categoryId = null, string? categoryType = null, CancellationToken token = default);

    /// <summary>
    /// 특정 채널의 봇 설정을 즉시 새로고침합니다.
    /// </summary>
    Task RefreshChannelAsync(string chzzkUid);

    /// <summary>
    /// [피닉스의 눈]: 스트리머 본인 계정의 토큰을 반환하고 필요시 갱신합니다.
    /// </summary>
    Task<string?> GetStreamerTokenAsync(StreamerProfile profile);

    /// <summary>
    /// [피닉스의 재건]: 현재 소켓 상태를 점검하고 필요시 최신 토큰으로 재연결을 수행합니다.
    /// </summary>
    Task EnsureConnectionAsync(string chzzkUid, bool forceFresh = false);

    /// <summary>
    /// [수동 정화]: 특정 채널에 걸린 모든 복구 잠금 및 실패 카운터를 즉시 초기화합니다.
    /// </summary>
    void CleanupRecoveryLock(string chzzkUid);
}
