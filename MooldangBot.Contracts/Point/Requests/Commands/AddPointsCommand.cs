using MediatR;
using MooldangBot.Contracts.Point.Enums;

namespace MooldangBot.Contracts.Point.Requests.Commands;

/// <summary>
/// 시청자에게 일반/후원 포인트를 적립하거나 차감합니다. (트랜잭션 로그 기록 포함)
/// 반환값: (성공 여부, 처리 후 남은 해당 재화 잔액)
/// </summary>
public record AddPointsCommand(
    string StreamerUid, 
    string ViewerUid, 
    string Nickname, 
    int Amount, 
    PointCurrencyType CurrencyType) : IRequest<(bool Success, int CurrentBalance)>;
