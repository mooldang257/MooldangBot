using MooldangBot.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

public interface IChzzkBotService
{
    /// <summary>
    /// 설정에 맞는 봇 또는 스트리머 토큰을 반환하고 필요시 갱신합니다.
    /// </summary>
    Task<string?> GetBotTokenAsync(StreamerProfile profile);

    /// <summary>
    /// 설정된 계정(봇/스트리머)으로 채팅 메시지를 전송합니다.
    /// </summary>
    Task<bool> SendReplyChatAsync(StreamerProfile profile, string message, CancellationToken token);

    /// <summary>
    /// 특정 채널의 봇 설정을 즉시 새로고침합니다.
    /// </summary>
    Task RefreshChannelAsync(string chzzkUid);
}
