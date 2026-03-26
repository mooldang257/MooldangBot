using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangAPI.Services;

public class PointTransactionService : IPointTransactionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PointTransactionService> _logger;

    public PointTransactionService(AppDbContext db, ILogger<PointTransactionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> GetBalanceAsync(string streamerUid, string viewerUid, CancellationToken ct = default)
    {
        var viewer = await _db.ViewerProfiles
            .FirstOrDefaultAsync(v => v.StreamerChzzkUid == streamerUid && v.ViewerUid == viewerUid, ct);
        return viewer?.Points ?? 0;
    }

    public async Task<(bool Success, int CurrentPoints)> AddPointsAsync(string streamerUid, string viewerUid, string nickname, int amount, CancellationToken ct = default)
    {
        int retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                var viewer = await _db.ViewerProfiles
                    .FirstOrDefaultAsync(v => v.StreamerChzzkUid == streamerUid && v.ViewerUid == viewerUid, ct);

                if (viewer == null)
                {
                    viewer = new ViewerProfile
                    {
                        StreamerChzzkUid = streamerUid,
                        ViewerUid = viewerUid,
                        Nickname = nickname,
                        Points = 0
                    };
                    _db.ViewerProfiles.Add(viewer);
                }
                else if (viewer.Nickname != nickname && !string.IsNullOrEmpty(nickname))
                {
                    viewer.Nickname = nickname;
                }

                viewer.Points += amount;
                
                // 포인트가 음수가 되지 않도록 방어 (필요 시)
                if (viewer.Points < 0) viewer.Points = 0;

                await _db.SaveChangesAsync(ct);
                return (true, viewer.Points);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                _logger.LogWarning($"⚠️ [포인트 트랜잭션 충돌] {nickname}님 재시도 중... ({retryCount}/3)");
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [포인트 트랜잭션 오류] {ex.Message}");
                return (false, 0);
            }
        }

        return (false, 0);
    }
}
