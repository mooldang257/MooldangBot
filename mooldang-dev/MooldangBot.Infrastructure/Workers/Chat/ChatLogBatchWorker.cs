using MooldangBot.Domain.Contracts.Chzzk;
using System.Collections.Concurrent;

using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooldangBot.Domain.DTOs;
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
    private readonly ConcurrentQueue<LogChatInteractions> _retryBuffer = new();

    protected override int DefaultIntervalSeconds => 1;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await RestoreBackupAsync();
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ProcessWorkAsync(CancellationToken ct)
    {
        var Settings = _optionsMonitor.Get(_workerName);
        await FlushAsync(Settings.MaxBatchSize, ct);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("⚠️ [{WorkerName}] 종료 감지. 잔여 로그를 저장합니다.", _workerName);
        bufferService.Complete();
        await FlushAsync(_optionsMonitor.Get(_workerName).MaxBatchSize, cancellationToken);
        await CreateBackupAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task FlushAsync(int MaxBatchSize, CancellationToken ct)
    {
        var Logs = new List<LogChatInteractions>();
        while (_retryBuffer.TryDequeue(out var RetryLog))
        {
            Logs.Add(RetryLog);
            if (Logs.Count >= MaxBatchSize) break;
        }
 
        if (Logs.Count < MaxBatchSize)
        {
            await foreach (var Log in bufferService.DrainAllAsync(ct))
            {
                Logs.Add(Log);
                if (Logs.Count >= MaxBatchSize) break;
            }
        }
 
        if (Logs.Count == 0) return;

        try
        {
            using var Scope = scopeFactory.CreateScope();
            var Db = Scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var DbConnection = Db.Database.GetDbConnection();
 
            if (DbConnection is not MySqlConnection MysqlConn)
            {
                _logger.LogError("❌ [{WorkerName}] 커넥션이 MySqlConnection이 아닙니다.", _workerName);
                foreach (var Log in Logs) _retryBuffer.Enqueue(Log);
                return;
            }
 
            if (MysqlConn.State != ConnectionState.Open)
                await MysqlConn.OpenAsync(ct);
 
            var DataTable = BuildDataTable(Logs);
            var BulkCopy = new MySqlBulkCopy(MysqlConn)
            {
                DestinationTableName = "LogChatInteractions",
                BulkCopyTimeout = 30
            };
 
            BulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(0, "StreamerProfileId"));
            BulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(1, "SenderNickname"));
            BulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(2, "Message"));
            BulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(3, "IsCommand"));
            BulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(4, "MessageType"));
            BulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(5, "CreatedAt"));
 
            await BulkCopy.WriteToServerAsync(DataTable, ct);
        }
        catch (Exception Ex)
        {
            _logger.LogError(Ex, "❌ [{WorkerName}] 벌크 적재 실패. {Count}건 대피.", _workerName, Logs.Count);
            foreach (var Log in Logs) _retryBuffer.Enqueue(Log);
        }
    }

    private static DataTable BuildDataTable(List<LogChatInteractions> Logs)
    {
        var Table = new DataTable();
        Table.Columns.Add("StreamerProfileId", typeof(int));
        Table.Columns.Add("SenderNickname", typeof(string));
        Table.Columns.Add("Message", typeof(string));
        Table.Columns.Add("IsCommand", typeof(bool));
        Table.Columns.Add("MessageType", typeof(string));
        Table.Columns.Add("CreatedAt", typeof(DateTime));
 
        foreach (var Log in Logs)
        {
            Table.Rows.Add(
                Log.StreamerProfileId,
                Log.SenderNickname,
                Log.Message,
                Log.IsCommand,
                Log.MessageType,
                Log.CreatedAt == default ? (DateTime)KstClock.Now : (DateTime)Log.CreatedAt
            );
        }
        return Table;
    }

    private async Task CreateBackupAsync()
    {
        if (_retryBuffer.IsEmpty) return;
        try
        {
            var Data = _retryBuffer.ToArray();
            var Json = JsonSerializer.Serialize(Data, ChzzkJsonContext.Default.LogChatInteractionsArray);
            var Dir = Path.GetDirectoryName(BackupFileName);
            if (!string.IsNullOrEmpty(Dir) && !Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
            await File.WriteAllTextAsync(BackupFileName, Json);
        }
        catch (Exception Ex)
        {
            _logger.LogCritical(Ex, "🚨 [{WorkerName}] 백업 파일 덤프 실패!", _workerName);
        }
    }

    private async Task RestoreBackupAsync()
    {
        if (!File.Exists(BackupFileName)) return;
        try
        {
            var Json = await File.ReadAllTextAsync(BackupFileName);
            var Data = JsonSerializer.Deserialize(Json, ChzzkJsonContext.Default.LogChatInteractionsArray);
            if (Data != null)
            {
                foreach (var Log in Data) _retryBuffer.Enqueue(Log);
                _logger.LogInformation("📦 [{WorkerName}] 파일에서 {Count}건 복원 완료.", _workerName, Data.Length);
            }
            File.Delete(BackupFileName);
        }
        catch (Exception Ex)
        {
            _logger.LogError(Ex, "❌ [{WorkerName}] 복구 중 오류 발생", _workerName);
        }
    }
}
