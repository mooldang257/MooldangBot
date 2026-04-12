using System;

namespace MooldangBot.ChzzkAPI.Contracts.Models.Events;

/// <summary>
/// [오시리스의 소포]: 치지직 이벤트를 담는 현대화된 봉투 모델입니다. (v3.7)
/// </summary>
public record ChzzkEventEnvelope(
    Guid MessageId,
    string ChzzkUid,
    ChzzkEventBase Payload,
    DateTime ReceivedAt,
    string Version = "3.7"
);
