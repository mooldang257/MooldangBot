using MediatR;

namespace MooldangBot.Contracts.Requests.Point.Commands;

/// <summary>
/// 잔액 부족 시 결제를 거부하는 원자적 차감 트랜잭션을 수행합니다. (후원 재화 전용)
/// 반환값: (차감 성공 여부, 현재 잔액)
/// </summary>
public record DeductDonationPointsCommand(
    string StreamerUid, 
    string ViewerUid, 
    int Amount) : IRequest<(bool Success, int CurrentBalance)>;
