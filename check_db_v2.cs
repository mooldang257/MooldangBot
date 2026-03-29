using Microsoft.EntityFrameworkCore;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Entities;

// ⚠️ DB 상태 점검용 스크립트 (Osiris Debugging)
// 이 스크립트는 AppDbContext를 활용해 실제 데이터 불일치를 확인합니다.

public class DbChecker
{
    private readonly AppDbContext _db;
    public DbChecker(AppDbContext db) => _db = db;

    public async Task RunAsync(string chzzkUid)
    {
        Console.WriteLine($"[DB CHECK] Target UID: {chzzkUid}");

        var omakaseItems = await _db.StreamerOmakases.IgnoreQueryFilters().Where(o => o.ChzzkUid == chzzkUid).ToListAsync();
        Console.WriteLine($"[OmakaseItems] Count: {omakaseItems.Count}");
        foreach(var o in omakaseItems)
        {
            Console.WriteLine($" - ID: {o.Id}, MenuId: {o.MenuId}, Icon: {o.Icon}");
        }

        var omakaseCmds = await _db.UnifiedCommands.IgnoreQueryFilters()
            .Where(c => c.ChzzkUid == chzzkUid && (c.FeatureType == "Omakase" || c.FeatureType == "Reply"))
            .ToListAsync();
        Console.WriteLine($"[UnifiedCommands] Count: {omakaseCmds.Count}");
        foreach(var c in omakaseCmds)
        {
            Console.WriteLine($" - ID: {c.Id}, Keyword: {c.Keyword}, TargetId: {c.TargetId}, FeatureType: {c.FeatureType}");
        }
    }
}
