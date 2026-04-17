using MediatR;
using MooldangBot.Contracts.Point.Requests.Models;
using MooldangBot.Contracts.Common.Interfaces;
using System.Collections.Generic;

namespace MooldangBot.Contracts.Point.Requests.Commands;

/// <summary>
/// 다수의 시청자 포인트를 한 번의 원자적 Dapper 트랜잭션으로 적재/차감합니다.
/// </summary>
public record BulkUpdatePointsCommand(IEnumerable<PointJob> Jobs) : IRequest;
