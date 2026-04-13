using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Features.Admin;

public class ChzzkCategorySyncService : IChzzkCategorySyncService
{
    public static bool IsRunning { get; private set; }
    public static DateTime? LastRunTime { get; private set; }
    public static string? LastResult { get; private set; }
    public static int LastAddedCount { get; private set; }
    public static int LastUpdatedCount { get; private set; }

    private readonly ILogger<ChzzkCategorySyncService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IChzzkApiClient _chzzkApi;

    public ChzzkCategorySyncService(ILogger<ChzzkCategorySyncService> logger, IServiceProvider serviceProvider, IChzzkApiClient chzzkApi)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _chzzkApi = chzzkApi;
    }

    public async Task SyncCategoriesAsync(CancellationToken stoppingToken)
    {
        IsRunning = true;
        try {
            _logger.LogInformation("🔄 [배치] 치지직 카테고리 동기화 시작...");

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            // [오시리스 v10.1]: 현재 DB에 등록된 별칭(Alias) 기반으로 최신 카테고리 정보 업데이트
            var aliases = await db.ChzzkCategoryAliases
                .Include(a => a.Category)
                .ToListAsync(stoppingToken);

            int updated = 0;
            foreach (var alias in aliases)
            {
                var result = await _chzzkApi.SearchCategoryAsync(alias.Alias);
                if (result?.Data?.Any() == true)
                {
                    var match = result.Data.FirstOrDefault(d => d.CategoryType == alias.Category?.CategoryType);
                    if (match != null && alias.Category != null)
                    {
                        alias.Category.CategoryValue = match.CategoryValue;
                        updated++;
                    }
                }
                await Task.Delay(200, stoppingToken); // API 과부하 방지
            }

            LastUpdatedCount = updated;
            LastRunTime = KstClock.Now;
            LastResult = $"성공 (업데이트: {updated})";
            await db.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [배치] 카테고리 동기화 중 오류 발생");
            LastResult = $"실패: {ex.Message}";
        }
        finally {
            IsRunning = false;
        }
    }

    public async Task<List<ChzzkCategory>> SearchAndSaveCategoryAsync(string keyword, CancellationToken ct = default)
    {
        var result = await _chzzkApi.SearchCategoryAsync(keyword);
        if (result?.Data == null) return new List<ChzzkCategory>();

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var savedCategories = new List<ChzzkCategory>();
        int added = 0;

        foreach (var data in result.Data)
        {
            var existing = await db.ChzzkCategories.FirstOrDefaultAsync(c => c.CategoryId == data.CategoryId && c.CategoryType == data.CategoryType, ct);
            if (existing == null)
            {
                var newCategory = new ChzzkCategory
                {
                    CategoryId = data.CategoryId,
                    CategoryType = data.CategoryType,
                    CategoryValue = data.CategoryValue,
                    UpdatedAt = KstClock.Now
                };
                db.ChzzkCategories.Add(newCategory);
                savedCategories.Add(newCategory);
                added++;
            }
            else
            {
                existing.CategoryValue = data.CategoryValue;
                existing.UpdatedAt = KstClock.Now;
                savedCategories.Add(existing);
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            LastAddedCount = added;
        }

        return savedCategories;
    }
}
