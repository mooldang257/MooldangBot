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
        var Duplicates = await db.TableCoreGlobalViewers
            .AsNoTracking()
            .GroupBy(v => v.ViewerUidHash)
            .Where(g => g.Count() > 1)
            .Select(g => new { 
                Hash = g.Key, 
                Ids = g.OrderBy(v => v.Id).Select(v => v.Id).ToList() 
            })
            .ToListAsync(ct);
 
        if (Duplicates.Count == 0)
        {
            logger.LogInformation("✅ [계정 통합] 중복된 시청자 데이터가 없습니다.");
            return Result<int>.Success(0);
        }
 
        int MergedCount = 0;
        var Connection = db.Database.GetDbConnection();
        if (Connection.State != ConnectionState.Open) await Connection.OpenAsync(ct);
 
        foreach (var Group in Duplicates)
        {
            var TargetId = Group.Ids[0];
            var SourceIds = Group.Ids.Skip(1).ToList();
            var SourceIdsStr = string.Join(",", SourceIds);
 
            logger.LogInformation("🔄 [계정 통합] Hash: {Hash} -> Target: {TargetId}, Sources: [{Sources}]", 
                Group.Hash, TargetId, SourceIdsStr);
 
            using var Transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                // A. 포인트 합산 및 통합 (FuncViewerPoints)
                // 1) Target에 이미 있는 스트리머의 포인트를 합산
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE FuncViewerPoints target
                    JOIN (
                        SELECT StreamerProfileId, SUM(Points) as TotalPoints
                        FROM FuncViewerPoints
                        WHERE GlobalViewerId IN ({SourceIdsStr})
                        GROUP BY StreamerProfileId
                    ) source ON target.StreamerProfileId = source.StreamerProfileId
                    SET target.Points = target.Points + source.TotalPoints, target.UpdatedAt = NOW()
                    WHERE target.GlobalViewerId = {TargetId}", ct);

                // 2) Target에는 없고 Source에만 있는 스트리머 데이터 이주
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE FuncViewerPoints
                    SET GlobalViewerId = {TargetId}, UpdatedAt = NOW()
                    WHERE GlobalViewerId IN ({SourceIdsStr})
                      AND StreamerProfileId NOT IN (
                          SELECT StreamerProfileId FROM (SELECT StreamerProfileId FROM FuncViewerPoints WHERE GlobalViewerId = {TargetId}) as t
                      )", ct);

                // 3) 남은 Source 포인트 레코드 삭제
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM FuncViewerPoints WHERE GlobalViewerId IN ({SourceIdsStr})", ct);

                // B. 후원 잔액 합산 및 통합 (FuncViewerDonations)
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE FuncViewerDonations target
                    JOIN (
                        SELECT StreamerProfileId, SUM(Balance) as TotalBalance, SUM(TotalDonated) as TotalDonated
                        FROM FuncViewerDonations
                        WHERE GlobalViewerId IN ({SourceIdsStr})
                        GROUP BY StreamerProfileId
                    ) source ON target.StreamerProfileId = source.StreamerProfileId
                    SET target.Balance = target.Balance + source.TotalBalance, 
                        target.TotalDonated = target.TotalDonated + source.TotalDonated,
                        target.UpdatedAt = NOW()
                    WHERE target.GlobalViewerId = {TargetId}", ct);

                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE FuncViewerDonations
                    SET GlobalViewerId = {TargetId}, UpdatedAt = NOW()
                    WHERE GlobalViewerId IN ({SourceIdsStr})
                      AND StreamerProfileId NOT IN (
                          SELECT StreamerProfileId FROM (SELECT StreamerProfileId FROM FuncViewerDonations WHERE GlobalViewerId = {TargetId}) as t
                      )", ct);

                await db.Database.ExecuteSqlRawAsync($"DELETE FROM FuncViewerDonations WHERE GlobalViewerId IN ({SourceIdsStr})", ct);

                // C. 스트리머 관계 통합 (CoreViewerRelations)
                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE CoreViewerRelations target
                    JOIN (
                        SELECT StreamerProfileId, SUM(AttendanceCount) as Att, SUM(ConsecutiveAttendanceCount) as Cons
                        FROM CoreViewerRelations
                        WHERE GlobalViewerId IN ({SourceIdsStr})
                        GROUP BY StreamerProfileId
                    ) source ON target.StreamerProfileId = source.StreamerProfileId
                    SET target.AttendanceCount = target.AttendanceCount + source.Att,
                        target.ConsecutiveAttendanceCount = GREATEST(target.ConsecutiveAttendanceCount, source.Cons)
                    WHERE target.GlobalViewerId = {TargetId}", ct);

                await db.Database.ExecuteSqlRawAsync($@"
                    UPDATE CoreViewerRelations
                    SET GlobalViewerId = {TargetId}
                    WHERE GlobalViewerId IN ({SourceIdsStr})
                      AND StreamerProfileId NOT IN (
                          SELECT StreamerProfileId FROM (SELECT StreamerProfileId FROM CoreViewerRelations WHERE GlobalViewerId = {TargetId}) as t
                      )", ct);

                await db.Database.ExecuteSqlRawAsync($"DELETE FROM CoreViewerRelations WHERE GlobalViewerId IN ({SourceIdsStr})", ct);

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
                    await db.Database.ExecuteSqlRawAsync($"UPDATE {table} SET GlobalViewerId = {TargetId} WHERE GlobalViewerId IN ({SourceIdsStr})", ct);
                }
 
                // E. 부 계정 삭제
                await db.Database.ExecuteSqlRawAsync($"DELETE FROM CoreGlobalViewers WHERE Id IN ({SourceIdsStr})", ct);
 
                await Transaction.CommitAsync(ct);
                MergedCount++;
            }
            catch (Exception Ex)
            {
                await Transaction.RollbackAsync(ct);
                logger.LogError(Ex, "❌ [계정 통합] Hash {Hash} 통합 중 오류 발생", Group.Hash);
            }
        }
 
        logger.LogInformation("✅ [계정 통합] 총 {Count}개의 중복 그룹이 통합되었습니다.", MergedCount);
        return Result<int>.Success(MergedCount);
    }

    private async Task DeduplicateSelfRecordsAsync(CancellationToken ct)
    {
        logger.LogInformation("🧹 [Self-Deduplication] 동일 계정 내 중복 레코드 정리를 시작합니다.");

        // A. 포인트 테이블 중복 정리
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE FuncViewerPoints target
            JOIN (
                SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId, SUM(Points) as TotalPoints
                FROM FuncViewerPoints
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON target.Id = source.TargetId
            SET target.Points = source.TotalPoints, target.UpdatedAt = NOW()", ct);
 
        await db.Database.ExecuteSqlRawAsync(@"
            DELETE p FROM FuncViewerPoints p
            JOIN (
                SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId
                FROM FuncViewerPoints
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON p.StreamerProfileId = source.StreamerProfileId AND p.GlobalViewerId = source.GlobalViewerId
            WHERE p.Id > source.TargetId", ct);

        // B. 후원 테이블 중복 정리
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE FuncViewerDonations target
            JOIN (
                SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId, SUM(Balance) as TotalBal, SUM(TotalDonated) as TotalDon
                FROM FuncViewerDonations
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON target.Id = source.TargetId
            SET target.Balance = source.TotalBal, target.TotalDonated = source.TotalDon, target.UpdatedAt = NOW()", ct);
 
        await db.Database.ExecuteSqlRawAsync(@"
            DELETE d FROM FuncViewerDonations d
            JOIN (
                SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId
                FROM FuncViewerDonations
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON d.StreamerProfileId = source.StreamerProfileId AND d.GlobalViewerId = source.GlobalViewerId
            WHERE d.Id > source.TargetId", ct);

        // C. 관계 테이블 중복 정리
        await db.Database.ExecuteSqlRawAsync(@"
            UPDATE CoreViewerRelations target
            JOIN (
                SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId, SUM(AttendanceCount) as Att
                FROM CoreViewerRelations
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON target.Id = source.TargetId
            SET target.AttendanceCount = source.Att", ct);

        await db.Database.ExecuteSqlRawAsync(@"
            DELETE r FROM CoreViewerRelations r
            JOIN (
                SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId
                FROM CoreViewerRelations
                GROUP BY StreamerProfileId, GlobalViewerId
                HAVING COUNT(*) > 1
            ) source ON r.StreamerProfileId = source.StreamerProfileId AND r.GlobalViewerId = source.GlobalViewerId
            WHERE r.Id > source.TargetId", ct);

        logger.LogInformation("✅ [Self-Deduplication] 중복 레코드 정리가 완료되었습니다.");
    }
}
