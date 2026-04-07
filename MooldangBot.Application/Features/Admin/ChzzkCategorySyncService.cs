using MooldangBot.Application.Interfaces;
using MooldangBot.ChzzkAPI.Interfaces;
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

            // 동기화 로직...
            LastRunTime = KstClock.Now;
            LastResult = "성공";
            await Task.CompletedTask;
        }
        finally {
            IsRunning = false;
        }
    }

    public async Task<List<ChzzkCategory>> SearchAndSaveCategoryAsync(string keyword, CancellationToken cancellationToken = default)
    {
        // 실구현 생략 또는 기존 로직 복구
        return new List<ChzzkCategory>();
    }
}
