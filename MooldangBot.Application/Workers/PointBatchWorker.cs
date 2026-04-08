using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [오버드라이브 워커]: 버퍼링된 채팅 포인트를 주기적으로 DB에 일관성 있게 저장하는 파수꾼입니다.
/// [v18.0] 익산 보험(Iksan Insurance): 장애 시 메모리 버퍼링 및 종료 시 파일 덤프를 통해 데이터 무손실을 보장합니다.
/// </summary>
public class PointBatchWorker(
    IPointBatchService batchService,
    IServiceScopeFactory scopeFactory,
    IPulseService pulse,
    IChaosManager chaosManager,
    ILogger<PointBatchWorker> logger) : BackgroundService
{
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(5);
    private const int MaxBatchSize = 2000;
    private const string BackupFileName = "data/temp_point_queue.json";
    
    // [익산 보험]: DB 적재 실패 시 임시 대기하는 버퍼
    private readonly ConcurrentQueue<PointJob> _retryBuffer = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [포인트 공명 워커] 가동 시작 (주기: 5초, 배치: {BatchSize})", MaxBatchSize);

        // 1. [익산 보험] 기동 시 미처리 파일 복구
        await RestoreBackupAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                pulse.ReportPulse("PointBatchWorker");
                await Task.Delay(_flushInterval, stoppingToken);
                await FlushAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [포인트 공명 워커] 주기적 플러시 중 예기치 못한 오류 발생");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning("⚠️ [포인트 공명 워커] 시스템 종료 감지. 마지막 잔여 포인트를 사수합니다.");

        batchService.Complete();
        await FlushAsync(cancellationToken);

        // 2. [익산 보험] 최종 잔여분 파일 덤프
        await CreateBackupAsync();

        await base.StopAsync(cancellationToken);
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        // [v18.0 심연의 시련] 가상 장애 시뮬레이션
        if (chaosManager.IsRedisPanic)
        {
            await foreach (var job in batchService.DrainAllAsync(ct))
            {
                _retryBuffer.Enqueue(job);
            }
            logger.LogWarning("🌪️ [심연의 시련] Redis 장애 모드: {Count}건의 포인트를 메모리 버퍼로 격리했습니다.", _retryBuffer.Count);
            return;
        }

        var jobs = new List<PointJob>();

        // 1. 리트라이 버퍼 우선 처리
        while (_retryBuffer.TryDequeue(out var retryJob))
        {
            jobs.Add(retryJob);
            if (jobs.Count >= MaxBatchSize) break;
        }

        // 2. 채널에서 신규 작업 적출 (남은 슬롯만큼)
        if (jobs.Count < MaxBatchSize)
        {
            await foreach (var job in batchService.DrainAllAsync(ct))
            {
                jobs.Add(job);
                if (jobs.Count >= MaxBatchSize) break;
            }
        }

        if (jobs.Count == 0) return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var pointService = scope.ServiceProvider.GetRequiredService<IPointTransactionService>();
            
            await pointService.BulkUpdatePointsAsync(jobs, ct);
            
            if (_retryBuffer.IsEmpty == false)
            {
                logger.LogInformation("✅ [Resonance Restored] 리트라이 버퍼 데이터 일부가 성공적으로 복구되었습니다. (잔여: {Count})", _retryBuffer.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [포인트 공명 적재 실패] {JobCount}건을 메모리 버퍼로 대피시킵니다.", jobs.Count);
            foreach (var job in jobs) _retryBuffer.Enqueue(job);
        }
    }

    private async Task CreateBackupAsync()
    {
        if (_retryBuffer.IsEmpty) return;

        try
        {
            var data = _retryBuffer.ToArray();
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(BackupFileName, json);
            logger.LogCritical("💾 [익산 보험] {Count}건의 미처리 포인트를 안전하게 파일({File})로 저장했습니다.", data.Length, BackupFileName);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "🚨 [익산 보험] 파일 덤프 실패! 포인트 유실 위험이 매우 높습니다.");
        }
    }

    private async Task RestoreBackupAsync()
    {
        // [v18.1] 전용 데이터 디렉토리 확인 및 생성
        var dir = Path.GetDirectoryName(BackupFileName);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        if (!File.Exists(BackupFileName)) return;

        try
        {
            var json = await File.ReadAllTextAsync(BackupFileName);
            var data = JsonSerializer.Deserialize<PointJob[]>(json);
            if (data != null)
            {
                foreach (var job in data) _retryBuffer.Enqueue(job);
                logger.LogInformation("📦 [익산 보험] 파일에서 {Count}건의 미처리 포인트를 복구하여 큐에 삽입했습니다.", data.Length);
            }
            File.Delete(BackupFileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [익산 보험] 백업 파일 복구 중 오류 발생");
        }
    }
}
