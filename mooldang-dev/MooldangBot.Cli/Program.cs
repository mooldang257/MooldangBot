using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Infrastructure.ApiClients.Philosophy;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;
using Dapper;

// 1. 환경 설정 및 .env 로드
string[] potentialPaths = { ".env", "../.env", "MooldangBot.Api/.env" };
string? foundPath = null;
foreach (var p in potentialPaths) { if (File.Exists(p)) { foundPath = Path.GetFullPath(p); break; } }

var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
if (foundPath != null) { 
    Env.Load(foundPath);
    foreach (var line in File.ReadAllLines(foundPath)) {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
        var split = trimmed.Split('=', 2);
        if (split.Length != 2) continue;
        var mappedKey = split[0].Trim().Replace("__", ":");
        overrides[mappedKey] = split[1].Trim().Trim('"', '\'');
    }
}
configBuilder.AddInMemoryCollection(overrides!);
var configuration = configBuilder.Build();
var connectionString = configuration.GetConnectionString("DefaultConnection") ?? configuration["ConnectionStrings:DefaultConnection"] ?? configuration["DEFAULT_CONNECTION"];

// 2. 호스트 DB 접속 처리 (localhost:3307 대응)
if (connectionString != null && connectionString.Contains("Server=db") && !File.Exists("/.dockerenv")) {
    connectionString = connectionString.Replace("Server=db", "Server=localhost");
    if (connectionString.Contains("3306")) {
        connectionString = connectionString.Replace("3306", "3307");
    } else if (!connectionString.Contains("Port=")) {
        connectionString += ";Port=3307";
    }
    Console.WriteLine("🌐 [네트워크]: 호스트 실행 감지 - DB 포인터를 localhost:3307로 전환합니다.");
}

// 3. 서비스 컬렉션 구성
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddHttpClient();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<IUserSession, SystemUserSession>();
services.AddSingleton<ILlmService, GeminiLlmService>();

services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("10.11-mariadb")));
services.AddDataProtection().SetApplicationName("MooldangBot").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "dp-keys")));

var serviceProvider = services.BuildServiceProvider();
var db = serviceProvider.GetRequiredService<AppDbContext>();
var llm = serviceProvider.GetRequiredService<ILlmService>();

// 4. 실행 인자 처리
var command = args.Length > 0 ? args[0].ToLower() : "cleanup";

try {
    if (command == "backfill-vectors")
    {
        await RunVectorBackfillAsync(db, llm);
    }
    else if (command == "migrate")
    {
        await RunMigrationAsync(db);
    }
    else
    {
        await RunDuplicateCleanupAsync(db);
    }
} catch (Exception ex) { 
    Console.WriteLine($"\n❌ [치명적 오류]: {ex.Message}"); 
}

async Task RunMigrationAsync(AppDbContext db)
{
    Console.WriteLine("\n🛠️ [시작] 데이터베이스 마이그레이션을 적용합니다...");
    await db.Database.MigrateAsync();
    Console.WriteLine("✅ [완료] 마이그레이션이 성공적으로 적용되었습니다.");
}

async Task RunVectorBackfillAsync(AppDbContext db, ILlmService llm)
{
    Console.WriteLine("\n🧠 [시작] 노래책 벡터(Vector) 주입 작업을 시작합니다...");
    
    // 1. 스트리머 노래책 (func_song_books) 백필
    var targetSongs = (await db.Database.GetDbConnection().QueryAsync<SongBook>(
        "SELECT Id, Title FROM FuncSongBooks WHERE TitleVector IS NULL AND IsDeleted = 0")).ToList();

    Console.WriteLine($"📊 [1/2] 개인 노래책 대상 선정: {targetSongs.Count}곡");
    int successCount = 0;

    foreach (var song in targetSongs)
    {
        try 
        {
            Console.Write($"   > '{song.Title}' 임베딩 생성 중... ");
            var vector = await llm.GetEmbeddingAsync(song.Title);
            if (vector.Length > 0)
            {
                if (successCount == 0) Console.Write($"[{vector.Length}d] ");
                
                var binaryVector = new byte[vector.Length * 4];
                Buffer.BlockCopy(vector, 0, binaryVector, 0, binaryVector.Length);
                
                await db.Database.GetDbConnection().ExecuteAsync(
                    "UPDATE FuncSongBooks SET TitleVector = @vector WHERE Id = @id", 
                    new { vector = binaryVector, id = song.Id });
                
                successCount++;
                Console.WriteLine("✅ 완료");
            }
            else
            {
                Console.WriteLine("⚠️ 실패 (결과 없음)");
            }
            await Task.Delay(1500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 오류: {ex.Message}");
        }
    }

    // 2. 스트리머 라이브러리 (func_song_streamer_library) 백필
    var targetLib = (await db.Database.GetDbConnection().QueryAsync<Streamer_SongLibrary>(
        "SELECT Id, Title FROM FuncSongStreamerLibrary WHERE TitleVector IS NULL")).ToList();

    Console.WriteLine($"\n📚 [2/2] 스트리머 라이브러리 대상 선정: {targetLib.Count}곡");
    int libSuccessCount = 0;

    foreach (var song in targetLib)
    {
        try 
        {
            Console.Write($"   > '{song.Title}' 임베딩 생성 중... ");
            var vector = await llm.GetEmbeddingAsync(song.Title);
            if (vector.Length > 0)
            {
                if (libSuccessCount == 0) Console.Write($"[{vector.Length}d] ");

                var binaryVector = new byte[vector.Length * 4];
                Buffer.BlockCopy(vector, 0, binaryVector, 0, binaryVector.Length);

                await db.Database.GetDbConnection().ExecuteAsync(
                    "UPDATE FuncSongStreamerLibrary SET TitleVector = @vector WHERE Id = @id",
                    new { vector = binaryVector, id = song.Id });
                
                libSuccessCount++;
                Console.WriteLine("✅ 완료");
            }
            else
            {
                Console.WriteLine("⚠️ 실패 (결과 없음)");
            }
            await Task.Delay(1500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 오류: {ex.Message}");
        }
    }

    Console.WriteLine($"\n🎉 [완료] 벡터 주입 완료! (개인: {successCount}, 라이브러리: {libSuccessCount})");
}

async Task RunDuplicateCleanupAsync(AppDbContext db)
{
    Console.WriteLine("\n🧹 [시작] 데이터베이스 중복 레코드 자체 통합 작업을 시작합니다...");

    // 1. 포인트 테이블
    Console.WriteLine("📊 [1/3] 포인트 테이블 정리 중...");
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE FuncViewerPoints target
        JOIN (
            SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId, SUM(Points) as total_points
            FROM FuncViewerPoints
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON target.Id = source.target_id
        SET target.Points = source.total_points, target.UpdatedAt = NOW()");

    int deletedPoints = await db.Database.ExecuteSqlRawAsync(@"
        DELETE p FROM FuncViewerPoints p
        JOIN (
            SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId
            FROM FuncViewerPoints
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON p.StreamerProfileId = source.StreamerProfileId AND p.GlobalViewerId = source.GlobalViewerId
        WHERE p.Id > source.target_id");
    Console.WriteLine($"   ✅ {deletedPoints}개의 중복 포인트 레코드가 제거되었습니다.");

    // 2. 후원 테이블
    Console.WriteLine("\n💰 [2/3] 후원 테이블 정리 중...");
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE FuncViewerDonations target
        JOIN (
            SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId, SUM(Balance) as total_bal, SUM(TotalDonated) as total_don
            FROM FuncViewerDonations
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON target.Id = source.target_id
        SET target.Balance = source.total_bal, target.TotalDonated = source.total_don, target.UpdatedAt = NOW()");

    int deletedDonations = await db.Database.ExecuteSqlRawAsync(@"
        DELETE d FROM FuncViewerDonations d
        JOIN (
            SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId
            FROM FuncViewerDonations
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON d.StreamerProfileId = source.StreamerProfileId AND d.GlobalViewerId = source.GlobalViewerId
        WHERE d.Id > source.target_id");
    Console.WriteLine($"   ✅ {deletedDonations}개의 중복 후원 레코드가 제거되었습니다.");

    // 3. 관계 테이블
    Console.WriteLine("\n🤝 [3/3] 관계 테이블 정리 중...");
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE CoreViewerRelations target
        JOIN (
            SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId, SUM(AttendanceCount) as att
            FROM CoreViewerRelations
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON target.Id = source.target_id
        SET target.AttendanceCount = source.att");

    int deletedRelations = await db.Database.ExecuteSqlRawAsync(@"
        DELETE r FROM CoreViewerRelations r
        JOIN (
            SELECT MIN(Id) as target_id, StreamerProfileId, GlobalViewerId
            FROM CoreViewerRelations
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON r.StreamerProfileId = source.StreamerProfileId AND r.GlobalViewerId = source.GlobalViewerId
        WHERE r.Id > source.target_id");
    Console.WriteLine($"   ✅ {deletedRelations}개의 중복 관계 레코드가 제거되었습니다.");

    Console.WriteLine("\n🎉 [완료] 모든 데이터베이스 중복 레코드가 성공적으로 통합되었습니다.");
}

public class SystemUserSession : IUserSession {
    public bool IsAuthenticated => true;
    public string? ChzzkUid => "SYSTEM";
    public string? Role => "master";
    public List<string> AllowedChannelIds => new();
}
