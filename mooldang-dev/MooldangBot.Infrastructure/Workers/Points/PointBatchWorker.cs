using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Domain.Common.Services;
using MooldangBot.Domain.Contracts.Chzzk;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Point;
using MediatR;
using MooldangBot.Modules.Point.Requests.Commands;

namespace MooldangBot.Infrastructure.Workers.Points;

/// <summary>
/// [오버드라이브 워커]: 버퍼링된 채팅 포인트를 주기적으로 DB에 일관성 있게 저장하는 파수꾼입니다.
/// </summary>
public class PointBatchWorker(IServiceProvider serviceProvider,
    IPointBatchService batchService,
    IServiceScopeFactory scopeFactory,
    ChaosManager chaosManager,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<PointBatchWorker> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(PointBatchWorker))
{
    private const string BackupFileName = "data/temp_point_queue.json";
    private readonly ConcurrentQueue<PointJob> _retryBuffer = new();

    protected override int DefaultIntervalSeconds => 5;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await RestoreBackupAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        var settings = _optionsMonitor.Get(_workerName);

        await FlushAsync(settings.MaxBatchSize, ct);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("⚠️ [{WorkerName}] 시스템 종료 감지. 마지막 잔여 포인트를 사수합니다.", _workerName);
        batchService.Complete();
        await FlushAsync(_optionsMonitor.Get(_workerName).MaxBatchSize, cancellationToken); 
        await CreateBackupAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task FlushAsync(int maxBatchSize, CancellationToken ct)
    {
        if (chaosManager.IsRedisPanic)
        {
            await foreach (var job in batchService.DrainAllAsync(ct))
            {
                _retryBuffer.Enqueue(job);
            }
            _logger.LogWarning("🌪️ [심연의 시련] Redis 장애 모드: {Count}건의 포인트를 메모리 버퍼로 격리했습니다.", _retryBuffer.Count);
            return;
        }

        var jobs = new List<PointJob>();
        while (_retryBuffer.TryDequeue(out var retryJob))
        {
            jobs.Add(retryJob);
            if (jobs.Count >= maxBatchSize) break;
        }

        if (jobs.Count < maxBatchSize)
        {
            await foreach (var job in batchService.DrainAllAsync(ct))
            {
                jobs.Add(job);
                if (jobs.Count >= maxBatchSize) break;
            }
        }

        if (jobs.Count == 0) return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            await mediator.Send(new BulkUpdatePointsCommand(jobs), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [포인트 공명 적재 실패] {JobCount}건을 메모리 버퍼로 대피시킵니다.", jobs.Count);
            foreach (var job in jobs) _retryBuffer.Enqueue(job);
        }
    }

    private async Task CreateBackupAsync()
    {
        if (_retryBuffer.IsEmpty) return;
        try
        {
            var data = _retryBuffer.ToArray();
            var json = JsonSerializer.Serialize(data, ChzzkJsonContext.Default.PointJobArray);
            await File.WriteAllTextAsync(BackupFileName, json);
            _logger.LogCritical("💾 [익산 보험] {Count}건의 미처리 포인트를 안전하게 파일({File})로 저장했습니다.", data.Length, BackupFileName);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "🚨 [익산 보험] 파일 덤프 실패!");
        }
    }

    private async Task RestoreBackupAsync()
    {
        var dir = Path.GetDirectoryName(BackupFileName);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        if (!File.Exists(BackupFileName)) return;
        try
        {
            var json = await File.ReadAllTextAsync(BackupFileName);
            var data = JsonSerializer.Deserialize(json, ChzzkJsonContext.Default.PointJobArray);
            if (data != null)
            {
                foreach (var job in data) _retryBuffer.Enqueue(job);
                _logger.LogInformation("📦 [익산 보험] 파일에서 {Count}건의 미처리 포인트를 복구했습니다.", data.Length);
            }
            File.Delete(BackupFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [익산 보험] 백업 파일 복구 중 오류 발생");
        }
    }
}