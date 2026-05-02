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
                // A. 포인트 합산 및 통합 (FuncViewerPoints)
                // 1) Target에 이미 있는 스트리머의 포인트를 합산
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE FuncViewerPoints target
                    JOIN (
                        SELECT StreamerProfileId, SUM(Points) as total_points
                        FROM FuncViewerPoints
                        WHERE GlobalViewerId IN ({sourceIdsStr})
                        GROUP BY StreamerProfileId
                    ) source ON target.StreamerProfileId = source.StreamerProfileId
                    SET target.Points = target.Points + source.total_points, target.UpdatedAt = NOW()
                    WHERE target.GlobalViewerId = {targetId}", ct);

                // 2) Target에는 없고 Source에만 있는 스트리머 데이터 이주
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE FuncViewerPoints
                    SET GlobalViewerId = {targetId}, UpdatedAt = NOW()
                    WHERE GlobalViewerId IN ({sourceIdsStr})
                      AND StreamerProfileId NOT IN (
                          SELECT StreamerProfileId FROM (SELECT StreamerProfileId FROM FuncViewerPoints WHERE GlobalViewerId = {targetId}) as t
                      )", ct);

                // 3) 남은 Source 포인트 레코드 삭제
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM FuncViewerPoints WHERE GlobalViewerId IN ({sourceIdsStr})", ct);

                // B. 후원 잔액 합산 및 통합 (FuncViewerDonations)
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE FuncViewerDonations target
                    JOIN (
                        SELECT StreamerProfileId, SUM(Balance) as total_balance, SUM(TotalDonated) as total_donated
                        FROM FuncViewerDonations
                        WHERE GlobalViewerId IN ({sourceIdsStr})
                        GROUP BY StreamerProfileId
                    ) source ON target.StreamerProfileId = source.StreamerProfileId
                    SET target.Balance = target.Balance + source.total_balance, 
                        target.TotalDonated = target.TotalDonated + source.total_donated,
                        target.UpdatedAt = NOW()
                    WHERE target.GlobalViewerId = {targetId}", ct);

                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE FuncViewerDonations
                    SET GlobalViewerId = {targetId}, UpdatedAt = NOW()
                    WHERE GlobalViewerId IN ({sourceIdsStr})
                      AND StreamerProfileId NOT IN (
                          SELECT StreamerProfileId FROM (SELECT StreamerProfileId FROM FuncViewerDonations WHERE GlobalViewerId = {targetId}) as t
                      )", ct);

                await db.Database.ExecuteSqlRawAsync($"DELETE FROM FuncViewerDonations WHERE GlobalViewerId IN ({sourceIdsStr})", ct);

                // C. 스트리머 관계 통합 (CoreViewerRelations)
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE CoreViewerRelations target
                    JOIN (
                        SELECT StreamerProfileId, SUM(AttendanceCount) as att, SUM(ConsecutiveAttendanceCount) as cons
                        FROM CoreViewerRelations
                        WHERE GlobalViewerId IN ({sourceIdsStr})
                        GROUP BY StreamerProfileId
                    ) source ON target.StreamerProfileId = source.StreamerProfileId
                    SET target.AttendanceCount = target.AttendanceCount + source.att,
                        target.ConsecutiveAttendanceCount = GREATEST(target.ConsecutiveAttendanceCount, source.cons)
                    WHERE target.GlobalViewerId = {targetId}", ct);

                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE CoreViewerRelations
                    SET GlobalViewerId = {targetId}
                    WHERE GlobalViewerId IN ({sourceIdsStr})
                      AND StreamerProfileId NOT IN (
                          SELECT StreamerProfileId FROM (SELECT StreamerProfileId FROM CoreViewerRelations WHERE GlobalViewerId = {targetId}) as t
                      )", ct);

                await db.Database.ExecuteSqlRawAsync($"DELETE FROM CoreViewerRelations WHERE GlobalViewerId IN ({sourceIdsStr})", ct);

                // D. 단순 이력 데이터 업데이트 (Foreign Key 변경)
                var logTables = new[] { 
                    "LogChatInteractions", 
                    "LogCommandExecutions", 
                    "LogPointTransactions", 
                    "FuncSongListQueues", 
                    "FuncViewerDonationHistories", 
                    "CoreStreamerManagers",
                    "LogRouletteResults",
                    "FuncRouletteSpins"
                };

                foreach (var table in logTables)
                {
                    await db.Database.ExecuteSqlRawAsync($"UPDATE {table} SET GlobalViewerId = {targetId} WHERE GlobalViewerId IN ({sourceIdsStr})", ct);
                }

                // E. 부 계정 삭제
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM CoreGlobalViewers WHERE Id IN ({sourceIdsStr})", ct);

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
            UPDATE FuncViewerPoints target
            JOIN (
                SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId, SUM(Points) as total_points
                FROM FuncViewerPoints
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON target.Id = source.target_id
            SET target.Points = source.total_points, target.UpdatedAt = NOW()", ct);

        await db.Database.ExecuteSqlRawAsync(@"
            DELETE p FROM FuncViewerPoints p
            JOIN (
                SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId
                FROM FuncViewerPoints
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON p.StreamerProfileId = source.StreamerProfileId AND p.GlobalViewerId = source.GlobalViewerId
            WHERE p.Id > source.target_id", ct);

        // B. 후원 테이블 중복 정리
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE FuncViewerDonations target
            JOIN (
                SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId, SUM(Balance) as total_bal, SUM(TotalDonated) as total_don
                FROM FuncViewerDonations
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON target.Id = source.target_id
            SET target.Balance = source.total_bal, target.TotalDonated = source.total_don, target.UpdatedAt = NOW()", ct);

        await db.Database.ExecuteSqlRawAsync(@"
            DELETE d FROM FuncViewerDonations d
            JOIN (
                SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId
                FROM FuncViewerDonations
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON d.StreamerProfileId = source.StreamerProfileId AND d.GlobalViewerId = source.GlobalViewerId
            WHERE d.Id > source.target_id", ct);

        // C. 관계 테이블 중복 정리
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE CoreViewerRelations target
            JOIN (
                SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId, SUM(AttendanceCount) as att
                FROM CoreViewerRelations
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON target.Id = source.target_id
            SET target.AttendanceCount = source.att", ct);

        await db.Database.ExecuteSqlRawAsync(@"
            DELETE r FROM CoreViewerRelations r
            JOIN (
                SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId
                FROM CoreViewerRelations
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON r.StreamerProfileId = source.StreamerProfileId AND r.GlobalViewerId = source.GlobalViewerId
            WHERE r.Id > source.target_id", ct);

        logger.LogInformation("✅ [Self-Deduplication] 중복 레코드 정리가 완료되었습니다.");
    }
}
