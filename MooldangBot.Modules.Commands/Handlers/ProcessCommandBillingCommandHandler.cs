using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Modules.Point.Interfaces;
using MooldangBot.Modules.Point.Requests.Commands;
using MooldangBot.Modules.Point.Enums;
using MooldangBot.Modules.Commands.Models;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Modules.Commands.Handlers;

/// <summary>
/// [세피로스의 집행]: 명령어 실행 전 통합 결제( Billing)를 처리하는 핸들러입니다.
/// (v4.0): 중복 차감 방지를 위해 Salvo당 1회만 호출됩니다.
/// </summary>
public class ProcessCommandBillingCommandHandler(
    ISender mediator,
    ILogger<ProcessCommandBillingCommandHandler> logger) : IRequestHandler<ProcessCommandBillingCommand, BillingResult>
{
    public async Task<BillingResult> Handle(ProcessCommandBillingCommand request, CancellationToken ct)
    {
        if (request.Cost <= 0) return new BillingResult(true);

        logger.LogInformation("💳 [Billing] Processing: {Cost} {Type} for {User}", request.Cost, request.CostType, request.StreamerUid);

        // [시니어 팁]: 포인트 모듈의 Deduct 명령어로 위임
        var currencyType = request.CostType == CommandCostType.Cheese 
            ? PointCurrencyType.DonationPoint 
            : PointCurrencyType.ChatPoint;

        var deductRequest = new DeductCurrencyCommand(
            request.StreamerUid,
            request.ViewerUid,
            request.ViewerNickname,
            request.Cost,
            currencyType,
            request.DonationAmount,
            request.AccumulateTotal);

        var result = await mediator.Send(deductRequest, ct);

        if (!result.Success)
        {
            logger.LogWarning("⚠️ [Billing] Failed: {Reason}", result.ErrorMessage);
            return new BillingResult(false, result.ErrorMessage);
        }

        return new BillingResult(true);
    }
}
