using MediatR;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.Commands.Requests;

/// <summary>
/// [오시리스의 재판]: 포인트 또는 후원 재화를 차감 요청하는 통합 명령입니다.
/// </summary>
public record ProcessCommandBillingCommand(
    string StreamerUid, 
    string ViewerUid, 
    int Cost, 
    CommandCostType CostType
) : IRequest<BillingResult>;

/// <summary>
/// 결제 처리 결과를 담는 DTO입니다.
/// </summary>
public record BillingResult(
    bool Success, 
    string? ErrorMessage = null, 
    int? RemainingBalance = null
);
