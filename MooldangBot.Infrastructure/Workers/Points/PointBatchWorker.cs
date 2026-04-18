using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Services;
using MooldangBot.Domain.Common.Services;
using MooldangBot.Domain.Contracts.Chzzk;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Point;
using MediatR;
using MooldangBot.Modules.Point.Requests.Commands;

// [수정] 네임스페이스를 Infrastructure.Workers로 통일
namespace MooldangBot.Infrastructure.Workers.Points;

/// <summary>
/// [오버드라이브 워커]: 버퍼링된 채팅 포인트를 주기적으로 DB에 일관성 있게 저장하는 파수꾼입니다.
/// [v18.0] 익산 보험(Iksan Insurance): 장애 시 메모리 버퍼링 및 종료 시 파일 덤프를 통해 데이터 무손실을 보장합니다.
/// </summary>
public class PointBatchWorker(
    IPointBatchService batchService,
    IServiceScopeFactory scopeFactory,
    PulseService pulse,
    ChaosManager chaosManager,
    IOptionsMonitor<WorkerSettings> optionsMonitor, // [수정] Named Options 패턴 적용
    ILogger<PointBatchWorker> logger) : BackgroundService
{
    private const string WorkerName = nameof(PointBatchWorker); // [추가] 옵션 키 명시
    private const string BackupFileName = "data/temp_point_queue.json";
    
    // [익산 보험]: DB 적재 실패 시 임시 대기하는 버퍼
    private readonly ConcurrentQueue<PointJob> _retryBuffer = new();

    // [수정] Named Options Get(WorkerName)으로 본인 설정을 획득
    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // [수정] 하드코딩 제거 및 설정값 동적 참조
        logger.LogInformation("🚀 [포인트 공명 워커] 가동 시작 (초기 주기: {Interval}초, 배치: {BatchSize})", 
            CurrentSettings.IntervalSeconds, CurrentSettings.MaxBatchSize);

        // 1. [익산 보험] 기동 시 미처리 파일 복구
        await RestoreBackupAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = CurrentSettings;

                // [추가] appsettings.json에서 IsEnabled를 false로 바꾸면 로직을 건너뜀 (핫리로딩)
                if (!settings.IsEnabled)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // 대기 모드
                    continue;
                }

                pulse.ReportPulse(WorkerName);
                
                // [수정] 동적으로 주기를 가져와 딜레이 적용
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
                await FlushAsync(settings.MaxBatchSize, stoppingToken); // 배치 사이즈 전달
            }
            catch (OperationCanceledException)
            {
                // [오시리스의 은신]: 정중한 작별 인사
                logger.LogInformation("👋 [{WorkerName}] 포인트 전송을 완료하고 안전하게 중단합니다.", WorkerName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [{WorkerName}] 주기적 플러시 중 예기치 못한 오류 발생", WorkerName);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning("⚠️ [{WorkerName}] 시스템 종료 감지. 마지막 잔여 포인트를 사수합니다.", WorkerName);

        batchService.Complete();
        // 종료 시에는 현재 설정된 최대 배치 사이즈 사용
        await FlushAsync(CurrentSettings.MaxBatchSize, cancellationToken); 

        // 2. [익산 보험] 최종 잔여분 파일 덤프
        await CreateBackupAsync();

        await base.StopAsync(cancellationToken);
    }

    // [수정] maxBatchSize를 파라미터로 받도록 변경
    private async Task FlushAsync(int maxBatchSize, CancellationToken ct)
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
            if (jobs.Count >= maxBatchSize) break;
        }

        // 2. 채널에서 신규 작업 적출 (남은 슬롯만큼)
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
            var json = JsonSerializer.Serialize(data, ChzzkJsonContext.Default.PointJobArray);
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
            var data = JsonSerializer.Deserialize(json, ChzzkJsonContext.Default.PointJobArray);
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