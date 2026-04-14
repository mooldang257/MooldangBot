using MediatR;
using MooldangBot.Contracts.Point.Enums;

namespace MooldangBot.Contracts.Point.Requests.Commands;

/// <summary>
/// [v7.0] 통합 재화 차감 요청: 포인트 또는 치즈를 원자적으로 차감합니다.
/// </summary>
/// <param name="StreamerUid">스트리머 고유 ID</param>
/// <param name="ViewerUid">시청자 고유 ID</param>
/// <param name="Amount">차감할 금액 (양수)</param>
/// <param name="CurrencyType">재화 유형 (ChatPoint 또는 DonationPoint)</param>
public record DeductCurrencyCommand(
    string StreamerUid, 
    string ViewerUid, 
    int Amount, 
    PointCurrencyType CurrencyType) : IRequest<DeductResult>;

/// <summary>
/// [v7.0] 재화 차감 결과 모델
/// </summary>
/// <param name="Success">성공 여부</param>
/// <param name="RemainingBalance">결과 잔액</param>
/// <param name="ErrorMessage">실패 시 에러 메시지</param>
public record DeductResult(bool Success, int RemainingBalance, string ErrorMessage = "");
