using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Abstractions;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;

// Duplicate Cleanup Script
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

if (connectionString != null && connectionString.Contains("Server=db") && !File.Exists("/.dockerenv")) {
    connectionString = connectionString.Replace("Server=db", "Server=localhost");
    // [물멍]: 호스트 OS에서 Docker DB에 접속할 때는 포트 3307을 사용합니다.
    if (connectionString.Contains("3306")) {
        connectionString = connectionString.Replace("3306", "3307");
    } else if (!connectionString.Contains("Port=")) {
        connectionString += ";Port=3307";
    }
    Console.WriteLine("🌐 [네트워크]: 호스트 실행 감지 - DB 포인터를 localhost:3307로 전환합니다.");
}

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddSingleton<IUserSession, SystemUserSession>();
services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("10.11-mariadb")).UseSnakeCaseNamingConvention());
services.AddDataProtection().SetApplicationName("MooldangBot").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "dp-keys")));

var serviceProvider = services.BuildServiceProvider();
var db = serviceProvider.GetRequiredService<AppDbContext>();

try {
    Console.WriteLine("\n🧹 [시작] 데이터베이스 중복 레코드 자체 통합 작업을 시작합니다...");

    // 1. 포인트 테이블 자체 중복 제거 (동일 계정 내 중복)
    Console.WriteLine("📊 [1/3] 포인트 테이블(func_viewer_points) 정리 중...");
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE func_viewer_points target
        JOIN (
            SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id, SUM(points) as total_points
            FROM func_viewer_points
            GROUP BY streamer_profile_id, global_viewer_id
            HAVING COUNT(*) > 1
        ) source ON target.id = source.target_id
        SET target.points = source.total_points, target.updated_at = NOW()");

    int deletedPoints = await db.Database.ExecuteSqlRawAsync(@"
        DELETE p FROM func_viewer_points p
        JOIN (
            SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id
            FROM func_viewer_points
            GROUP BY streamer_profile_id, global_viewer_id
            HAVING COUNT(*) > 1
        ) source ON p.streamer_profile_id = source.streamer_profile_id AND p.global_viewer_id = source.global_viewer_id
        WHERE p.id > source.target_id");
    Console.WriteLine($"   ✅ {deletedPoints}개의 중복 포인트 레코드가 제거되었습니다.");

    // 2. 후원 테이블 자체 중복 제거
    Console.WriteLine("\n💰 [2/3] 후원 테이블(func_viewer_donations) 정리 중...");
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE func_viewer_donations target
        JOIN (
            SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id, SUM(balance) as total_bal, SUM(total_donated) as total_don
            FROM func_viewer_donations
            GROUP BY streamer_profile_id, global_viewer_id
            HAVING COUNT(*) > 1
        ) source ON target.id = source.target_id
        SET target.balance = source.total_bal, target.total_donated = source.total_don, target.updated_at = NOW()");

    int deletedDonations = await db.Database.ExecuteSqlRawAsync(@"
        DELETE d FROM func_viewer_donations d
        JOIN (
            SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id
            FROM func_viewer_donations
            GROUP BY streamer_profile_id, global_viewer_id
            HAVING COUNT(*) > 1
        ) source ON d.streamer_profile_id = source.streamer_profile_id AND d.global_viewer_id = source.global_viewer_id
        WHERE d.id > source.target_id");
    Console.WriteLine($"   ✅ {deletedDonations}개의 중복 후원 레코드가 제거되었습니다.");

    // 3. 관계 테이블 자체 중복 제거
    Console.WriteLine("\n🤝 [3/3] 관계 테이블(core_viewer_relations) 정리 중...");
    await db.Database.ExecuteSqlRawAsync(@"
        UPDATE core_viewer_relations target
        JOIN (
            SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id, SUM(attendance_count) as att
            FROM core_viewer_relations
            GROUP BY streamer_profile_id, global_viewer_id
            HAVING COUNT(*) > 1
        ) source ON target.id = source.target_id
        SET target.attendance_count = source.att");

    int deletedRelations = await db.Database.ExecuteSqlRawAsync(@"
        DELETE r FROM core_viewer_relations r
        JOIN (
            SELECT MIN(id) as target_id, streamer_profile_id, global_viewer_id
            FROM core_viewer_relations
            GROUP BY streamer_profile_id, global_viewer_id
            HAVING COUNT(*) > 1
        ) source ON r.streamer_profile_id = source.streamer_profile_id AND r.global_viewer_id = source.global_viewer_id
        WHERE r.id > source.target_id");
    Console.WriteLine($"   ✅ {deletedRelations}개의 중복 관계 레코드가 제거되었습니다.");

    Console.WriteLine("\n🎉 [완료] 모든 데이터베이스 중복 레코드가 성공적으로 통합되었습니다.");
} catch (Exception ex) { Console.WriteLine($"\n❌ [오류]: {ex.Message}"); }

public class SystemUserSession : IUserSession {
    public bool IsAuthenticated => true;
    public string? ChzzkUid => "SYSTEM";
    public string? Role => "master";
    public List<string> AllowedChannelIds => new();
}
