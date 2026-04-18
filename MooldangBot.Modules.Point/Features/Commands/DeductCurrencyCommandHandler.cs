using MediatR;
using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Modules.Point.Enums;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.Point.Features.Commands;

/// <summary>
/// [오시리스의 재판]: 명령어 실행 전용 통합 결제 핸들러입니다.
/// </summary>
public class CommandBillingHandler : IRequestHandler<ProcessCommandBillingCommand, BillingResult>
{
    private readonly ISender _mediator;

    public CommandBillingHandler(ISender mediator)
    {
        _mediator = mediator;
    }

    public async Task<BillingResult> Handle(ProcessCommandBillingCommand request, CancellationToken ct)
    {
        if (request.Cost <= 0) return new BillingResult(true);

        if (request.CostType == CommandCostType.Cheese)
        {
            // 1. 치즈 포인트 차감 (후원금)
            var (success, _) = await _mediator.Send(new DeductDonationPointsCommand(request.StreamerUid, request.ViewerUid, request.Cost), ct);
            
            if (!success)
            {
                return new BillingResult(false, "후원 잔액이 부족합니다.");
            }
            return new BillingResult(true);
        }
        else if (request.CostType == CommandCostType.Point)
        {
            // 2. 채팅 포인트 차감
            var (success, currentPoints) = await _mediator.Send(new AddPointsCommand(
                request.StreamerUid, request.ViewerUid, "System", -request.Cost, PointCurrencyType.ChatPoint), ct);
            
            if (!success)
            {
                return new BillingResult(false, "채팅 포인트가 부족합니다.");
            }
            return new BillingResult(true, null, currentPoints);
        }

        return new BillingResult(false, "지원되지 않는 재화 타입입니다.");
    }
}
