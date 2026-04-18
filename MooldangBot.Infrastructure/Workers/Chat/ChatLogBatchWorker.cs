using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using MooldangBot.Application.Contracts.Chzzk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Application.Contracts.Chzzk;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MySqlConnector;

namespace MooldangBot.Infrastructure.Workers.Chat;

/// <summary>
/// [오시리스의 서기 워커 v2.0]: 버퍼링된 채팅 로그를 MySqlBulkCopy(LOAD DATA LOCAL INFILE)로 초고속 적재합니다.
/// (P0: 성능): Dapper 동적 SQL 생성 → MySqlBulkCopy 전환으로 GC 부하 및 SQL 파싱 오버헤드를 극한으로 제거했습니다.
/// </summary>
public class ChatLogBatchWorker(
    IChatLogBufferService bufferService,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor, // [수정] Named Options 패턴 적용
    ILogger<ChatLogBatchWorker> logger) : BackgroundService
{
    private const string WorkerName = nameof(ChatLogBatchWorker);
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1); // 기본 1초
    private const string BackupFileName = "data/temp_chat_logs.json";

    // [익산 보험 - 저우선순위]: 장애 시 메모리에 임시 보관
    private readonly ConcurrentQueue<ChatInteractionLog> _retryBuffer = new();

    // [수정] Named Options Get(WorkerName)으로 본인 설정을 획득
    private WorkerSettings CurrentSettings => optionsMonitor.Get(WorkerName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [채팅 로그 배치 워커] 가동 시작 (설정: {Interval}s, 배치: {BatchSize})", 
            CurrentSettings.IntervalSeconds, CurrentSettings.MaxBatchSize);

        // 기동 시 백업 복구 시도
        await RestoreBackupAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = CurrentSettings;
            if (!settings.IsEnabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            try
            {
                await FlushAsync(settings.MaxBatchSize, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("👋 [채팅 로그 워커] 기록을 안전하게 저장하고 중단합니다.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ [채팅 로그 워커] 플러시 중 오류 발생");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning("⚠️ [채팅 로그 워커] 종료 감지. 잔여 로그를 저장합니다.");

        bufferService.Complete();
        await FlushAsync(CurrentSettings.MaxBatchSize, cancellationToken);

        // [저우선순위 백업]: 종료 시점에 여전히 남은 데이터가 있다면 파일로 저장
        await CreateBackupAsync();

        await base.StopAsync(cancellationToken);
    }

    private async Task FlushAsync(int maxBatchSize, CancellationToken ct)
    {
        var logs = new List<ChatInteractionLog>();

        // 1. 리트라이 버퍼 우선 처리
        while (_retryBuffer.TryDequeue(out var retryLog))
        {
            logs.Add(retryLog);
            if (logs.Count >= maxBatchSize) break;
        }

        // 2. 채널에서 데이터 적출
        if (logs.Count < maxBatchSize)
        {
            await foreach (var log in bufferService.DrainAllAsync(ct))
            {
                logs.Add(log);
                if (logs.Count >= maxBatchSize) break;
            }
        }

        if (logs.Count == 0) return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var dbConnection = db.Database.GetDbConnection();

            // [v2.0 오시리스의 일격]: MySqlBulkCopy — LOAD DATA LOCAL INFILE 프로토콜 사용
            if (dbConnection is not MySqlConnection mysqlConn)
            {
                logger.LogError("❌ [채팅 로그 워커] 커넥션이 MySqlConnection이 아닙니다. 폴백 불가.");
                foreach (var log in logs) _retryBuffer.Enqueue(log);
                return;
            }

            if (mysqlConn.State != ConnectionState.Open)
                await mysqlConn.OpenAsync(ct);

            // DataTable 구성: DB 테이블 컬럼 순서와 정확히 일치시킴
            var dataTable = BuildDataTable(logs);

            var bulkCopy = new MySqlBulkCopy(mysqlConn)
            {
                DestinationTableName = "log_chat_interactions",
                BulkCopyTimeout = 30
            };

            // 컬럼 매핑: DataTable 컬럼 인덱스 → DB 테이블 컬럼명
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(0, "streamer_profile_id"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(1, "sender_nickname"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(2, "message"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(3, "is_command"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(4, "message_type"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(5, "created_at"));

            var result = await bulkCopy.WriteToServerAsync(dataTable, ct);

            logger.LogDebug("🌊 [채팅 로그 적재] {Count}건을 MySqlBulkCopy로 적재 완료 (Warnings: {Warnings})",
                logs.Count, result.Warnings?.Count ?? 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [채팅 로그 벌크 적재 실패] {Count}건을 메모리 버퍼로 대피시킵니다.", logs.Count);
            foreach (var log in logs) _retryBuffer.Enqueue(log);
        }
    }

    /// <summary>
    /// [오시리스의 서판]: ChatInteractionLog 리스트를 DataTable로 변환합니다.
    /// DataTable은 MySqlBulkCopy가 내부적으로 LOAD DATA INFILE 스트리밍에 사용하는 입력 형식입니다.
    /// </summary>
    private static DataTable BuildDataTable(List<ChatInteractionLog> logs)
    {
        var table = new DataTable();
        table.Columns.Add("streamer_profile_id", typeof(int));
        table.Columns.Add("sender_nickname", typeof(string));
        table.Columns.Add("message", typeof(string));
        table.Columns.Add("is_command", typeof(bool));
        table.Columns.Add("message_type", typeof(string));
        table.Columns.Add("created_at", typeof(DateTime));

        foreach (var log in logs)
        {
            table.Rows.Add(
                log.StreamerProfileId,
                log.SenderNickname,
                log.Message,
                log.IsCommand,
                log.MessageType,
                log.CreatedAt == default ? (DateTime)KstClock.Now : (DateTime)log.CreatedAt
            );
        }

        return table;
    }

    private async Task CreateBackupAsync()
    {
        if (_retryBuffer.IsEmpty) return;

        try
        {
            // [저우선순위 지침 반영]: RAM 가용량 확인 로직이 실현 불가능하므로, 일단 시도하되 실패 시 과감히 포기
            var data = _retryBuffer.ToArray();
            // [P0 Quick Win] Source Gen 경로: 리플렉션 제거로 GC 부하 감소
            var json = JsonSerializer.Serialize(data, ChzzkJsonContext.Default.ChatInteractionLogArray);
            
            var dir = Path.GetDirectoryName(BackupFileName);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(BackupFileName, json);
            logger.LogInformation("💾 [채팅 로그 백업] {Count}건을 파일로 저장했습니다.", data.Length);
        }
        catch (Exception ex)
        {
            // 실패하더라도 루프를 돌지 않고 로그만 남기고 종료 (안정성 우선)
            logger.LogCritical(ex, "🚨 [채팅 로그 백업 실패] 리소스 부족 또는 오류로 인해 데이터를 보존하지 못했습니다.");
        }
    }

    private async Task RestoreBackupAsync()
    {
        if (!File.Exists(BackupFileName)) return;

        try
        {
            var json = await File.ReadAllTextAsync(BackupFileName);
            // [P0 Quick Win] Source Gen 경로: 리플렉션 제거로 GC 부하 감소
            var data = JsonSerializer.Deserialize(json, ChzzkJsonContext.Default.ChatInteractionLogArray);
            if (data != null)
            {
                foreach (var log in data) _retryBuffer.Enqueue(log);
                logger.LogInformation("📦 [채팅 로그 복구] 파일에서 {Count}건을 복원했습니다.", data.Length);
            }
            File.Delete(BackupFileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [채팅 로그 복구 오류]");
        }
    }
}
