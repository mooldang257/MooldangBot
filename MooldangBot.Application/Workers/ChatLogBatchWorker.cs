using System.Collections.Concurrent;
using System.Text.Json;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Workers;

/// <summary>
/// [오시리스의 서기 워커]: 버퍼링된 채팅 로그를 주기적으로 MariaDB에 벌크 인서트하는 파수꾼입니다.
/// </summary>
public class ChatLogBatchWorker(
    IChatLogBufferService bufferService,
    IServiceScopeFactory scopeFactory,
    ILogger<ChatLogBatchWorker> logger) : BackgroundService
{
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1); // 1초 주기로 플러시
    private const int MaxBatchSize = 5000; // 최대 5천 건 단위로 벌크 인서트
    private const string BackupFileName = "data/temp_chat_logs.json";

    // [익산 보험 - 저우선순위]: 장애 시 메모리에 임시 보관
    private readonly ConcurrentQueue<ChatInteractionLog> _retryBuffer = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 [채팅 로그 배치 워커] 가동 시작 (주기: 1초, 배치: {BatchSize})", MaxBatchSize);

        // 기동 시 백업 복구 시도
        await RestoreBackupAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_flushInterval, stoppingToken);
                await FlushAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // [오시리스의 은신]: 정중한 종료
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
        await FlushAsync(cancellationToken);

        // [저우선순위 백업]: 종료 시점에 여전히 남은 데이터가 있다면 파일로 저장
        await CreateBackupAsync();

        await base.StopAsync(cancellationToken);
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        var logs = new List<ChatInteractionLog>();

        // 1. 리트라이 버퍼 우선 처리
        while (_retryBuffer.TryDequeue(out var retryLog))
        {
            logs.Add(retryLog);
            if (logs.Count >= MaxBatchSize) break;
        }

        // 2. 채널에서 데이터 적출
        if (logs.Count < MaxBatchSize)
        {
            await foreach (var log in bufferService.DrainAllAsync(ct))
            {
                logs.Add(log);
                if (logs.Count >= MaxBatchSize) break;
            }
        }

        if (logs.Count == 0) return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var connection = db.Database.GetDbConnection();

            // [오시리스의 일격]: Dapper를 사용한 벌크 인서트 SQL 생성
            // MariaDB/MySQL 최적화 문법: INSERT INTO ... VALUES (...), (...);
            var sqlHead = "INSERT INTO log_chat_interactions (streamer_profile_id, sender_nickname, message, is_command, message_type, created_at) VALUES ";
            var valuesList = new List<string>();
            var parameters = new DynamicParameters();

            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];
                valuesList.Add($"(@S{i}, @N{i}, @M{i}, @C{i}, @T{i}, @A{i})");
                parameters.Add($"S{i}", log.StreamerProfileId);
                parameters.Add($"N{i}", log.SenderNickname);
                parameters.Add($"M{i}", log.Message);
                parameters.Add($"C{i}", log.IsCommand ? 1 : 0);
                parameters.Add($"T{i}", log.MessageType);
                parameters.Add($"A{i}", log.CreatedAt == default ? KstClock.Now : log.CreatedAt);
            }

            var finalSql = sqlHead + string.Join(", ", valuesList) + ";";

            await connection.ExecuteAsync(finalSql, parameters);
            
            logger.LogDebug("🌊 [채팅 로그 적재] {Count}건의 로그를 벌크 저장했습니다.", logs.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [채팅 로그 벌크 적재 실패] {Count}건을 메모리 버퍼로 대피시킵니다.", logs.Count);
            foreach (var log in logs) _retryBuffer.Enqueue(log);
        }
    }

    private async Task CreateBackupAsync()
    {
        if (_retryBuffer.IsEmpty) return;

        try
        {
            // [저우선순위 지침 반영]: RAM 가용량 확인 로직이 실현 불가능하므로, 일단 시도하되 실패 시 과감히 포기
            var data = _retryBuffer.ToArray();
            var json = JsonSerializer.Serialize(data);
            
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
            var data = JsonSerializer.Deserialize<ChatInteractionLog[]>(json);
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
