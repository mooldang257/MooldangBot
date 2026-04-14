using MooldangBot.Contracts.Point.Requests.Queries;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Contracts.Point.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Security;

namespace MooldangBot.Modules.Point.Features.Queries.GetBalance;

public class GetBalanceHandler : IRequestHandler<GetBalanceQuery, int>
{
    private readonly IPointDbContext _db;

    public GetBalanceHandler(IPointDbContext db)
    {
        _db = db;
    }

    public async Task<int> Handle(GetBalanceQuery request, CancellationToken ct)
    {
        var viewerHash = Sha256Hasher.ComputeHash(request.ViewerUid);
        
        var viewer = await _db.StreamerViewers
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.StreamerProfile!.ChzzkUid == request.StreamerUid && v.GlobalViewer!.ViewerUidHash == viewerHash, ct);
            
        if (viewer == null) return 0;
        
        return request.CurrencyType == PointCurrencyType.ChatPoint 
            ? viewer.Points 
            : viewer.DonationPoints;
    }
}
