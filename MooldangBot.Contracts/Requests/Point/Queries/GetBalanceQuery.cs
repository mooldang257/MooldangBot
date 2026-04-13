using MediatR;
using MooldangBot.Contracts.Enums;

namespace MooldangBot.Contracts.Requests.Point.Queries;

/// <summary>
/// 특정 시청자의 재화(일반 채팅 포인트 혹은 후원 치즈 포인트) 잔액을 조회합니다.
/// </summary>
public record GetBalanceQuery(
    string StreamerUid, 
    string ViewerUid, 
    PointCurrencyType CurrencyType) : IRequest<int>;
