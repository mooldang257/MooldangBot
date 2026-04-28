using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common.Models;
using System.Data;

namespace MooldangBot.Application.Features.Identity.Commands;

/// <summary>
/// [계정 통합 명령]: 동일한 UID 해시를 가진 중복 시청자 데이터를 하나로 통합합니다.
/// </summary>
public record MergeDuplicateViewersCommand : IRequest<Result<int>>;

public class MergeDuplicateViewersCommandHandler(
    IAppDbContext db,
    ILogger<MergeDuplicateViewersCommandHandler> logger) : IRequestHandler<MergeDuplicateViewersCommand, Result<int>>
{
    public async Task<Result<int>> Handle(MergeDuplicateViewersCommand request, CancellationToken ct)
    {
        logger.LogInformation("🚀 [계정 통합] 중복 데이터(계정 및 레코드) 식별 및 통합 작업을 시작합니다.");

        // 0. [Self-Deduplication] 동일 계정 내의 레코드 중복 먼저 정리
        await DeduplicateSelfRecordsAsync(ct);

        // 1. 중복된 UID 해시 그룹 식별
        var duplicates = await db.CoreGlobalViewers
            .AsNoTracking()
            .GroupBy(v => v.ViewerUidHash)
            .Where(g => g.Count() > 1)
            .Select(g => new { 
                Hash = g.Key, 
                Ids = g.OrderBy(v => v.Id).Select(v => v.Id).ToList() 
            })
            .ToListAsync(ct);

        if (duplicates.Count == 0)
        {
            logger.LogInformation("✅ [계정 통합] 중복된 시청자 데이터가 없습니다.");
            return Result<int>.Success(0);
        }

        int mergedCount = 0;
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);

        foreach (var group in duplicates)
        {
            var targetId = group.Ids[0];
            var sourceIds = group.Ids.Skip(1).ToList();
            var sourceIdsStr = string.Join(",", sourceIds);

            logger.LogInformation("🔄 [계정 통합] Hash: {Hash} -> Target: {TargetId}, Sources: [{Sources}]", 
                group.Hash, targetId, sourceIdsStr);

            using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                // A. 포인트 합산 및 통합 (func_viewer_points)
                // 1) Target에 이미 있는 스트리머의 포인트를 합산
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE func_viewer_points target
                    JOIN (
                        SELECT streamer_profile_id, SUM(points) as total_points
                        FROM func_viewer_points
                        WHERE global_viewer_id IN ({sourceIdsStr})
                        GROUP BY streamer_profile_id
                    ) source ON target.streamer_profile_id = source.streamer_profile_id
                    SET target.points = target.points + source.total_points, target.updated_at = NOW()
                    WHERE target.global_viewer_id = {targetId}", ct);

                // 2) Target에는 없고 Source에만 있는 스트리머 데이터 이주
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE func_viewer_points
                    SET global_viewer_id = {targetId}, updated_at = NOW()
                    WHERE global_viewer_id IN ({sourceIdsStr})
                      AND streamer_profile_id NOT IN (
                          SELECT streamer_profile_id FROM (SELECT streamer_profile_id FROM func_viewer_points WHERE global_viewer_id = {targetId}) as t
                      )", ct);

                // 3) 남은 Source 포인트 레코드 삭제
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM func_viewer_points WHERE global_viewer_id IN ({sourceIdsStr})", ct);

                // B. 후원 잔액 합산 및 통합 (func_viewer_donations)
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE func_viewer_donations target
                    JOIN (
                        SELECT streamer_profile_id, SUM(balance) as total_balance, SUM(total_donated) as total_donated
                        FROM func_viewer_donations
                        WHERE global_viewer_id IN ({sourceIdsStr})
                        GROUP BY streamer_profile_id
                    ) source ON target.streamer_profile_id = source.streamer_profile_id
                    SET target.balance = target.balance + source.total_balance, 
                        target.total_donated = target.total_donated + source.total_donated,
                        target.updated_at = NOW()
                    WHERE target.global_viewer_id = {targetId}", ct);

                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE func_viewer_donations
                    SET global_viewer_id = {targetId}, updated_at = NOW()
                    WHERE global_viewer_id IN ({sourceIdsStr})
                      AND streamer_profile_id NOT IN (
                          SELECT streamer_profile_id FROM (SELECT streamer_profile_id FROM func_viewer_donations WHERE global_viewer_id = {targetId}) as t
                      )", ct);

                await db.Database.ExecuteSqlRawAsync($"DELETE FROM func_viewer_donations WHERE global_viewer_id IN ({sourceIdsStr})", ct);

                // C. 스트리머 관계 통합 (core_viewer_relations)
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE core_viewer_relations target
                    JOIN (
                        SELECT streamer_profile_id, SUM(attendance_count) as att, SUM(consecutive_attendance_count) as cons
                        FROM core_viewer_relations
                        WHERE global_viewer_id IN ({sourceIdsStr})
                        GROUP BY streamer_profile_id
                    ) source ON target.streamer_profile_id = source.streamer_profile_id
                    SET target.attendance_count = target.attendance_count + source.att,
                        target.consecutive_attendance_count = GREATEST(target.consecutive_attendance_count, source.cons)
                    WHERE target.global_viewer_id = {targetId}", ct);

                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE core_viewer_relations
                    SET global_viewer_id = {targetId}
                    WHERE global_viewer_id IN ({sourceIdsStr})
                      AND streamer_profile_id NOT IN (
                          SELECT streamer_profile_id FROM (SELECT streamer_profile_id FROM core_viewer_relations WHERE global_viewer_id = {targetId}) as t
                      )", ct);

                await db.Database.ExecuteSqlRawAsync($"DELETE FROM core_viewer_relations WHERE global_viewer_id IN ({sourceIdsStr})", ct);

                // D. 단순 이력 데이터 업데이트 (Foreign Key 변경)
                var logTables = new[] { 
                    "log_chat_interactions", 
                    "log_command_executions", 
                    "log_point_transactions", 
                    "func_song_list_queues", 
                    "func_viewer_donation_histories", 
                    "core_streamer_managers",
                    "log_roulette_results",
                    "func_roulette_spins"
                };

                foreach (var table in logTables)
                {
                    await db.Database.ExecuteSqlRawAsync($"UPDATE {table} SET global_viewer_id = {targetId} WHERE global_viewer_id IN ({sourceIdsStr})", ct);
                }

                // E. 부 계정 삭제
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM core_global_viewers WHERE id IN ({sourceIdsStr})", ct);

                await transaction.CommitAsync(ct);
                mergedCount++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                logger.LogError(ex, "❌ [계정 통합] Hash {Hash} 통합 중 오류 발생", group.Hash);
            }
        }

        logger.LogInformation("✅ [계정 통합] 총 {Count}개의 중복 그룹이 통합되었습니다.", mergedCount);
        return Result<int>.Success(mergedCount);
    }

    private async Task DeduplicateSelfRecordsAsync(CancellationToken ct)
    {
        logger.LogInformation("🧹 [Self-Deduplication] 동일 계정 내 중복 레코드 정리를 시작합니다.");

        // A. 포인트 테이블 중복 정리
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE func_viewer_points target
            JOIN (
                SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id, SUM(points) as total_points
                FROM func_viewer_points
                GROUP BY streamer_profile_id, global_viewer_id
                HAVING COUNT(*) > 1
            ) source ON target.id = source.target_id
            SET target.points = source.total_points, target.updated_at = NOW()", ct);

        await db.Database.ExecuteSqlRawAsync(@"
            DELETE p FROM func_viewer_points p
            JOIN (
                SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id
                FROM func_viewer_points
                GROUP BY streamer_profile_id, global_viewer_id
                HAVING COUNT(*) > 1
            ) source ON p.streamer_profile_id = source.streamer_profile_id AND p.global_viewer_id = source.global_viewer_id
            WHERE p.id > source.target_id", ct);

        // B. 후원 테이블 중복 정리
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE func_viewer_donations target
            JOIN (
                SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id, SUM(balance) as total_bal, SUM(total_donated) as total_don
                FROM func_viewer_donations
                GROUP BY streamer_profile_id, global_viewer_id
                HAVING COUNT(*) > 1
            ) source ON target.id = source.target_id
            SET target.balance = source.total_bal, target.total_donated = source.total_don, target.updated_at = NOW()", ct);

        await db.Database.ExecuteSqlRawAsync(@"
            DELETE d FROM func_viewer_donations d
            JOIN (
                SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id
                FROM func_viewer_donations
                GROUP BY streamer_profile_id, global_viewer_id
                HAVING COUNT(*) > 1
            ) source ON d.streamer_profile_id = source.streamer_profile_id AND d.global_viewer_id = source.global_viewer_id
            WHERE d.id > source.target_id", ct);

        // C. 관계 테이블 중복 정리
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE core_viewer_relations target
            JOIN (
                SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id, SUM(attendance_count) as att
                FROM core_viewer_relations
                GROUP BY streamer_profile_id, global_viewer_id
                HAVING COUNT(*) > 1
            ) source ON target.id = source.target_id
            SET target.attendance_count = source.att", ct);

        await db.Database.ExecuteSqlRawAsync(@"
            DELETE r FROM core_viewer_relations r
            JOIN (
                SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id
                FROM core_viewer_relations
                GROUP BY streamer_profile_id, global_viewer_id
                HAVING COUNT(*) > 1
            ) source ON r.streamer_profile_id = source.streamer_profile_id AND r.global_viewer_id = source.global_viewer_id
            WHERE r.id > source.target_id", ct);

        logger.LogInformation("✅ [Self-Deduplication] 중복 레코드 정리가 완료되었습니다.");
    }
}
