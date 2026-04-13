using System;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Models;

/// <summary>
/// [v2.2] 봇 엔진에서 발행되는 원본 이벤트 항목입니다.
/// </summary>
public sealed record ChatEventItem(
    Guid MessageId,      // [v2.2] 추적 및 멱등성 체크용 ID
    string ChzzkUid,
    string JsonPayload,
    KstClock ReceivedAt,
    string Version = "2.2" // [v2.2] 규격 버전 관리
);
