using MooldangBot.Contracts.Point.Requests.Queries;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Contracts.Point.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Security;

namespace MooldangBot.Modules.Point.Features.Queries.GetBalance;

/// <summary>
/// [v7.0] 하이브리드 잔액 조회기: MariaDB의 확정 잔액과 Redis의 미확정 변동분을 
/// 합산하여 사용자에게 실시간성을 보장하는 정확한 잔액을 반환합니다.
/// </summary>
public class GetBalanceHandler : IRequestHandler<GetBalanceQuery, int>
{
    private readonly IPointDbContext _db;
    private readonly IPointCacheService _pointCache;

    public GetBalanceHandler(IPointDbContext db, IPointCacheService pointCache)
    {
        _db = db;
        _pointCache = pointCache;
    }

    public async Task<int> Handle(GetBalanceQuery request, CancellationToken ct)
    {
        var viewerHash = Sha256Hasher.ComputeHash(request.ViewerUid);

        if (request.CurrencyType == PointCurrencyType.ChatPoint)
        {
            // [무료 포인트]: DB 확정치 + Redis 증분치 합산
            var dbBalance = await _db.ViewerPoints
                .AsNoTracking()
                .Where(v => v.StreamerProfile!.ChzzkUid == request.StreamerUid && v.GlobalViewer!.ViewerUidHash == viewerHash)
                .Select(v => v.Points)
                .FirstOrDefaultAsync(ct);

            var redisIncrement = await _pointCache.GetIncrementalPointAsync(request.StreamerUid, request.ViewerUid);

            return dbBalance + redisIncrement;
        }
        else
        {
            // [유료 재화]: MariaDB 실시간 잔액 반환
            return await _db.ViewerDonations
                .AsNoTracking()
                .Where(v => v.StreamerProfile!.ChzzkUid == request.StreamerUid && v.GlobalViewer!.ViewerUidHash == viewerHash)
                .Select(v => v.Balance)
                .FirstOrDefaultAsync(ct);
        }
    }
}
