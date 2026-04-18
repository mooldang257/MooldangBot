using MooldangBot.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace MooldangBot.Contracts.Common.Interfaces;

public interface IChzzkCategorySyncService
{
    static bool IsRunning { get; }
    static DateTime? LastRunTime { get; }
    static string? LastResult { get; }
    static int LastAddedCount { get; }
    static int LastUpdatedCount { get; }

    Task SyncCategoriesAsync(CancellationToken stoppingToken);
    Task<List<ChzzkCategory>> SearchAndSaveCategoryAsync(string keyword, CancellationToken cancellationToken = default);
}
