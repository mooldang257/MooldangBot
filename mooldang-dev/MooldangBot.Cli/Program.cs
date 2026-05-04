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
string[] PotentialPaths = { ".env", "../.env", "MooldangBot.Api/.env" };
string? FoundPath = null;
foreach (var P in PotentialPaths) { if (File.Exists(P)) { FoundPath = Path.GetFullPath(P); break; } }

var ConfigBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
var Overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
if (FoundPath != null) { 
    Env.Load(FoundPath);
    foreach (var Line in File.ReadAllLines(FoundPath)) {
        var Trimmed = Line.Trim();
        if (string.IsNullOrEmpty(Trimmed) || Trimmed.StartsWith("#")) continue;
        var Split = Trimmed.Split('=', 2);
        if (Split.Length != 2) continue;
        var MappedKey = Split[0].Trim().Replace("__", ":");
        Overrides[MappedKey] = Split[1].Trim().Trim('"', '\'');
    }
}
ConfigBuilder.AddInMemoryCollection(Overrides!);
var Configuration = ConfigBuilder.Build();
var ConnectionString = Configuration.GetConnectionString("DefaultConnection") ?? Configuration["ConnectionStrings:DefaultConnection"] ?? Configuration["DEFAULT_CONNECTION"];

// 2. 호스트 DB 접속 처리 (localhost:3307 대응)
if (ConnectionString != null && ConnectionString.Contains("Server=db") && !File.Exists("/.dockerenv")) {
    ConnectionString = ConnectionString.Replace("Server=db", "Server=localhost");
    if (ConnectionString.Contains("3306")) {
        ConnectionString = ConnectionString.Replace("3306", "3307");
    } else if (!ConnectionString.Contains("Port=")) {
        ConnectionString += ";Port=3307";
    }
    Console.WriteLine("🌐 [네트워크]: 호스트 실행 감지 - DB 포인터를 localhost:3307로 전환합니다.");
}

// 3. 서비스 컬렉션 구성
var Services = new ServiceCollection();
Services.AddLogging(builder => builder.AddConsole());
Services.AddHttpClient();
Services.AddSingleton<IConfiguration>(Configuration);
Services.AddSingleton<IUserSession, SystemUserSession>();
Services.AddSingleton<ILlmService, GeminiLlmService>();

Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(ConnectionString, ServerVersion.Parse("10.11-mariadb")));
Services.AddDataProtection().SetApplicationName("MooldangBot").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "dp-keys")));

var ServiceProvider = Services.BuildServiceProvider();
var Db = ServiceProvider.GetRequiredService<AppDbContext>();
var Llm = ServiceProvider.GetRequiredService<ILlmService>();

// 4. 실행 인자 처리
var Command = args.Length > 0 ? args[0].ToLower() : "cleanup";

try {
    if (Command == "backfill-vectors")
    {
        await RunVectorBackfillAsync(Db, Llm);
    }
    else if (Command == "migrate")
    {
        await RunMigrationAsync(Db);
    }
    else
    {
        await RunDuplicateCleanupAsync(Db);
    }
} catch (Exception Ex) { 
    Console.WriteLine($"\n❌ [치명적 오류]: {Ex.Message}"); 
}

async Task RunMigrationAsync(AppDbContext db)
{
    Console.WriteLine("\n🛠️ [시작] 데이터베이스 마이그레이션을 적용합니다...");
    await db.Database.MigrateAsync();
    Console.WriteLine("✅ [완료] 마이그레이션이 성공적으로 적용되었습니다.");
}

async Task RunVectorBackfillAsync(AppDbContext Db, ILlmService Llm)
{
    Console.WriteLine("\n🧠 [시작] 노래책 벡터(Vector) 주입 작업을 시작합니다...");
    
    // 1. 스트리머 노래책 (FuncSongBooks) 백필
    var TargetSongs = (await Db.Database.GetDbConnection().QueryAsync<FuncSongBooks>(
        "SELECT Id, Title FROM FuncSongBooks WHERE TitleVector IS NULL AND IsDeleted = 0")).ToList();

    Console.WriteLine($"📊 [1/2] 개인 노래책 대상 선정: {TargetSongs.Count}곡");
    int SuccessCount = 0;

    foreach (var Song in TargetSongs)
    {
        try 
        {
            Console.Write($"   > '{Song.Title}' 임베딩 생성 중... ");
            var Vector = await Llm.GetEmbeddingAsync(Song.Title);
            if (Vector.Length > 0)
            {
                if (SuccessCount == 0) Console.Write($"[{Vector.Length}d] ");
                
                var BinaryVector = new byte[Vector.Length * 4];
                Buffer.BlockCopy(Vector, 0, BinaryVector, 0, BinaryVector.Length);
                
                await Db.Database.GetDbConnection().ExecuteAsync(
                    "UPDATE FuncSongBooks SET TitleVector = @vector WHERE Id = @id", 
                    new { vector = BinaryVector, id = Song.Id });
                
                SuccessCount++;
                Console.WriteLine("✅ 완료");
            }
            else
            {
                Console.WriteLine("⚠️ 실패 (결과 없음)");
            }
            await Task.Delay(1500);
        }
        catch (Exception Ex)
        {
            Console.WriteLine($"❌ 오류: {Ex.Message}");
        }
    }

    // 2. 스트리머 라이브러리 (FuncSongStreamerLibrary) 백필
    var TargetLib = (await Db.Database.GetDbConnection().QueryAsync<FuncSongStreamerLibrary>(
        "SELECT Id, Title FROM FuncSongStreamerLibrary WHERE TitleVector IS NULL")).ToList();

    Console.WriteLine($"\n📚 [2/2] 스트리머 라이브러리 대상 선정: {TargetLib.Count}곡");
    int LibSuccessCount = 0;

    foreach (var Song in TargetLib)
    {
        try 
        {
            Console.Write($"   > '{Song.Title}' 임베딩 생성 중... ");
            var Vector = await Llm.GetEmbeddingAsync(Song.Title);
            if (Vector.Length > 0)
            {
                if (LibSuccessCount == 0) Console.Write($"[{Vector.Length}d] ");

                var BinaryVector = new byte[Vector.Length * 4];
                Buffer.BlockCopy(Vector, 0, BinaryVector, 0, BinaryVector.Length);

                await Db.Database.GetDbConnection().ExecuteAsync(
                    "UPDATE FuncSongStreamerLibrary SET TitleVector = @vector WHERE Id = @id",
                    new { vector = BinaryVector, id = Song.Id });
                
                LibSuccessCount++;
                Console.WriteLine("✅ 완료");
            }
            else
            {
                Console.WriteLine("⚠️ 실패 (결과 없음)");
            }
            await Task.Delay(1500);
        }
        catch (Exception Ex)
        {
            Console.WriteLine($"❌ 오류: {Ex.Message}");
        }
    }

    Console.WriteLine($"\n🎉 [완료] 벡터 주입 완료! (개인: {SuccessCount}, 라이브러리: {LibSuccessCount})");
}

async Task RunDuplicateCleanupAsync(AppDbContext db)
{
    Console.WriteLine("\n🧹 [시작] 데이터베이스 중복 레코드 자체 통합 작업을 시작합니다...");

    // 1. 포인트 테이블
    Console.WriteLine("📊 [1/3] 포인트 테이블 정리 중...");
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE FuncViewerPoints target
        JOIN (
            SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId, SUM(Points) as TotalPoints
            FROM FuncViewerPoints
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON target.Id = source.TargetId
        SET target.Points = source.TotalPoints, target.UpdatedAt = NOW()");

    int DeletedPoints = await db.Database.ExecuteSqlRawAsync(@"
        DELETE p FROM FuncViewerPoints p
        JOIN (
            SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId
            FROM FuncViewerPoints
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON p.StreamerProfileId = source.StreamerProfileId AND p.GlobalViewerId = source.GlobalViewerId
        WHERE p.Id > source.TargetId");
    Console.WriteLine($"   ✅ {DeletedPoints}개의 중복 포인트 레코드가 제거되었습니다.");

    // 2. 후원 테이블
    Console.WriteLine("\n💰 [2/3] 후원 테이블 정리 중...");
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE FuncViewerDonations target
        JOIN (
            SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId, SUM(Balance) as TotalBal, SUM(TotalDonated) as TotalDon
            FROM FuncViewerDonations
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON target.Id = source.TargetId
        SET target.Balance = source.TotalBal, target.TotalDonated = source.TotalDon, target.UpdatedAt = NOW()");

    int DeletedDonations = await db.Database.ExecuteSqlRawAsync(@"
        DELETE d FROM FuncViewerDonations d
        JOIN (
            SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId
            FROM FuncViewerDonations
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON d.StreamerProfileId = source.StreamerProfileId AND d.GlobalViewerId = source.GlobalViewerId
        WHERE d.Id > source.TargetId");
    Console.WriteLine($"   ✅ {DeletedDonations}개의 중복 후원 레코드가 제거되었습니다.");

    // 3. 관계 테이블
    Console.WriteLine("\n🤝 [3/3] 관계 테이블 정리 중...");
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE CoreViewerRelations target
        JOIN (
            SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId, SUM(AttendanceCount) as Att
            FROM CoreViewerRelations
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON target.Id = source.TargetId
        SET target.AttendanceCount = source.Att");

    int DeletedRelations = await db.Database.ExecuteSqlRawAsync(@"
        DELETE r FROM CoreViewerRelations r
        JOIN (
            SELECT MIN(Id) as TargetId, StreamerProfileId, GlobalViewerId
            FROM CoreViewerRelations
            GROUP BY StreamerProfileId, GlobalViewerId
            HAVING COUNT(*) > 1
        ) source ON r.StreamerProfileId = source.StreamerProfileId AND r.GlobalViewerId = source.GlobalViewerId
        WHERE r.Id > source.TargetId");
    Console.WriteLine($"   ✅ {DeletedRelations}개의 중복 관계 레코드가 제거되었습니다.");

    Console.WriteLine("\n🎉 [완료] 모든 데이터베이스 중복 레코드가 성공적으로 통합되었습니다.");
}

public class SystemUserSession : IUserSession {
    public bool IsAuthenticated => true;
    public string? ChzzkUid => "SYSTEM";
    public string? Role => "master";
    public List<string> AllowedChannelIds => new();
}
