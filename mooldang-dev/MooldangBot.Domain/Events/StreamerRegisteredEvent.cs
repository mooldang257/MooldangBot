using MediatR;

namespace MooldangBot.Domain.Events;

/// <summary>
/// [오시리스의 탄생]: 신규 스트리머가 시스템에 최초로 등록되었을 때 발생하는 도메인 이벤트입니다.
/// </summary>
/// <param name="ChzzkUid">네이버 치지직 고유 채널 ID</param>
/// <param name="ChannelName">채널 활동명</param>
public record StreamerRegisteredEvent(string ChzzkUid, string ChannelName) : INotification;
