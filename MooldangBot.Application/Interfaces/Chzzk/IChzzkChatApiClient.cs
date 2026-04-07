using MooldangBot.Domain.DTOs;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces.Chzzk;

/// <summary>
/// [오시리스의 전령]: 치지직 채팅 전송 및 세션 관리를 위한 저수준 API 인터페이스입니다.
/// </summary>
public interface IChzzkChatApiClient
{
    Task<bool> SendChatMessageAsync(string accessToken, string channelId, string message);
    Task<bool> SendChatNoticeAsync(string accessToken, string channelId, string message);
    Task<ChzzkSessionAuthResponse?> GetSessionAuthAsync(string accessToken);
    Task<bool> SubscribeEventAsync(string accessToken, string sessionKey, string eventType, string channelId);
}
