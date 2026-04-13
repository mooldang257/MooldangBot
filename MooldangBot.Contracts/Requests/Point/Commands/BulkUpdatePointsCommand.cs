using MediatR;
using MooldangBot.Contracts.Requests.Point.Models;
using MooldangBot.Contracts.Interfaces.MediatR;
using System.Collections.Generic;

namespace MooldangBot.Contracts.Requests.Point.Commands;

/// <summary>
/// 다수의 시청자 포인트를 한 번의 원자적 Dapper 트랜잭션으로 적재/차감합니다.
/// IPerformanceCriticalRequest 마커를 활용하여 로깅 등 무거운 파이프라인에서 생략됩니다.
/// </summary>
public record BulkUpdatePointsCommand(IEnumerable<PointJob> Jobs) : IRequest, IPerformanceCriticalRequest;
