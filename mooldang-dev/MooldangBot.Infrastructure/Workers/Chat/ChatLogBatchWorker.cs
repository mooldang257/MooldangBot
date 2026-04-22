using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.Contracts.Chzzk;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MySqlConnector;

namespace MooldangBot.Infrastructure.Workers.Chat;

/// <summary>
/// [오시리스의 서기 워커]: 버퍼링된 채팅 로그를 MySqlBulkCopy로 초고속 적재합니다.
/// </summary>
public class ChatLogBatchWorker(IServiceProvider serviceProvider,
    
    IChatLogBufferService bufferService,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<WorkerSettings> optionsMonitor,
    ILogger<ChatLogBatchWorker> logger) : BaseHybridWorker(serviceProvider, logger, optionsMonitor, nameof(ChatLogBatchWorker))
{
    private const string BackupFileName = "data/temp_chat_logs.json";
    private readonly ConcurrentQueue<ChatInteractionLog> _retryBuffer = new();

    protected override int DefaultIntervalSeconds => 1;

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
        _logger.LogWarning("⚠️ [{WorkerName}] 종료 감지. 잔여 로그를 저장합니다.", _workerName);
        bufferService.Complete();
        await FlushAsync(_optionsMonitor.Get(_workerName).MaxBatchSize, cancellationToken);
        await CreateBackupAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task FlushAsync(int maxBatchSize, CancellationToken ct)
    {
        var logs = new List<ChatInteractionLog>();
        while (_retryBuffer.TryDequeue(out var retryLog))
        {
            logs.Add(retryLog);
            if (logs.Count >= maxBatchSize) break;
        }

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

            if (dbConnection is not MySqlConnection mysqlConn)
            {
                _logger.LogError("❌ [{WorkerName}] 커넥션이 MySqlConnection이 아닙니다.", _workerName);
                foreach (var log in logs) _retryBuffer.Enqueue(log);
                return;
            }

            if (mysqlConn.State != ConnectionState.Open)
                await mysqlConn.OpenAsync(ct);

            var dataTable = BuildDataTable(logs);
            var bulkCopy = new MySqlBulkCopy(mysqlConn)
            {
                DestinationTableName = "log_chat_interactions",
                BulkCopyTimeout = 30
            };

            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(0, "streamer_profile_id"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(1, "sender_nickname"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(2, "message"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(3, "is_command"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(4, "message_type"));
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(5, "created_at"));

            await bulkCopy.WriteToServerAsync(dataTable, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [{WorkerName}] 벌크 적재 실패. {Count}건 대피.", _workerName, logs.Count);
            foreach (var log in logs) _retryBuffer.Enqueue(log);
        }
    }

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
            var data = _retryBuffer.ToArray();
            var json = JsonSerializer.Serialize(data, ChzzkJsonContext.Default.ChatInteractionLogArray);
            var dir = Path.GetDirectoryName(BackupFileName);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(BackupFileName, json);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "🚨 [{WorkerName}] 백업 파일 덤프 실패!", _workerName);
        }
    }

    private async Task RestoreBackupAsync()
    {
        if (!File.Exists(BackupFileName)) return;
        try
        {
            var json = await File.ReadAllTextAsync(BackupFileName);
            var data = JsonSerializer.Deserialize(json, ChzzkJsonContext.Default.ChatInteractionLogArray);
            if (data != null)
            {
                foreach (var log in data) _retryBuffer.Enqueue(log);
                _logger.LogInformation("📦 [{WorkerName}] 파일에서 {Count}건 복원 완료.", _workerName, data.Length);
            }
            File.Delete(BackupFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [{WorkerName}] 복구 중 오류 발생", _workerName);
        }
    }
}
