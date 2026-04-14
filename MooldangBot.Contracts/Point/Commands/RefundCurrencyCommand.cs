using MediatR;
using System;

namespace MooldangBot.Contracts.Point.Commands;

/// <summary>
/// [오시리스의 자율 복구]: 명령어 실행 실패 시 차감된 재화를 원래대로 되돌리는 환불 명령입니다. (v7.4 Contracts 정립)
/// </summary>
public record RefundCurrencyCommand(
    string StreamerUid,
    string ViewerUid,
    string ViewerNickname,
    int Amount,
    string CostType,
    string Reason,
    Guid CorrelationId
) : IRequest<bool>;
