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
using MooldangBot.Contracts.Common.Interfaces;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;


// 1. [파로스의 자각]: .env 파일 탐색
string[] potentialPaths = { 
    ".env", 
    "../.env",
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "MooldangBot.Api", ".env"),
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MooldangBot.Api", ".env"),
    "MooldangBot.Api/.env"
};

string? foundPath = null;
foreach (var p in potentialPaths)
{
    if (File.Exists(p)) { foundPath = Path.GetFullPath(p); break; }
}

var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

if (foundPath != null)
{
    Console.WriteLine($"[파로스의 자각]: 설정 파일 로드 완료 - {foundPath}");
    Env.Load(foundPath);
    
    foreach (var line in File.ReadAllLines(foundPath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
        var split = trimmed.Split('=', 2);
        if (split.Length != 2) continue;
        var key = split[0].Trim();
        var val = split[1].Trim();
        
        // [방어적 고도화]: 값 양 끝의 따옴표(" 또는 ') 제거
        if (val.Length >= 2 && ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'"))))
        {
            val = val.Substring(1, val.Length - 2);
        }
        
        // 1. [표준 정문화]: __를 :로 변환하여 Configuration에 주입
        var mappedKey = key.Replace("__", ":");
        overrides[mappedKey] = val;

        // 2. [PascalCase 통합]: ALL_CAPS_SNAKE를 PascalCase로 변환하여 추가 주입
        // 예: CHZZK_API:CLIENT_ID -> ChzzkApi:ClientId
        var pascalKey = string.Join(":", mappedKey.Split(':').Select(section => 
            string.Join("", section.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1).ToLower() : p))));
        
        if (pascalKey != mappedKey)
        {
            overrides[pascalKey] = val;
        }

        // 시스템 환경 변수로도 가용하게 노출
        System.Environment.SetEnvironmentVariable(key, val);
    }
}

configBuilder.AddInMemoryCollection(overrides!);
var configuration = configBuilder.Build();

var connectionString = configuration.GetConnectionString("DefaultConnection") 
                      ?? configuration["ConnectionStrings:DefaultConnection"]
                      ?? configuration["CONNECTIONSTRINGS:DEFAULT_CONNECTION"]
                      ?? configuration["DEFAULT_CONNECTION"];

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("❌ [오시리스의 거절]: 연결 문자열(DefaultConnection)을 확보하지 못했습니다.");
    return;
}

// 🌐 [네트워크 오버라이드]: 호스트 OS에서 실행 시 'db'를 'localhost'로 전환
if (connectionString.Contains("Server=db") && !File.Exists("/.dockerenv"))
{
    connectionString = connectionString.Replace("Server=db", "Server=localhost");
    Console.WriteLine("🌐 [네트워크]: 호스트 실행 감지 - DB 포인터를 localhost로 전환합니다.");
}

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());

// 🔐 CLI용 시스템 세션 등록 (DbContext 의존성 해결)
services.AddSingleton<IUserSession, SystemUserSession>();

services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("10.11-mariadb"), 
        mySqlOptions => 
        {
            mySqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
            mySqlOptions.EnableRetryOnFailure();
        })
        .UseSnakeCaseNamingConvention());

// 🔐 [보안 강화]: API 서버와 동일한 방식으로 데이터 보호 키 저장소 및 서비스 등록
services.AddDataProtection()
    .SetApplicationName("MooldangBot")
    .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"));

var serviceProvider = services.BuildServiceProvider();
var db = serviceProvider.GetRequiredService<AppDbContext>();

try
{
    Console.WriteLine("\n🌊 [MooldangBot.Seeder] DB 초기화 및 멱등성 보정 작업을 시작합니다...");

    // 2. 마이그레이션 자동화
    Console.WriteLine("⚙️ [1/3] 마이그레이션 상태 확인 중...");
    var pending = await db.Database.GetPendingMigrationsAsync();
    if (pending.Any())
    {
        await db.Database.MigrateAsync();
        Console.WriteLine($"   ✅ {pending.Count()}개의 마이그레이션이 반영되었습니다.");
    }
    else
    {
        Console.WriteLine("   ℹ️ 최신 버전입니다.");
    }

    // 3. 명령어 마스터 데이터는 이제 코드 레지스트리(Registry)에서 관리됩니다. (DB 시딩 생략)
    Console.WriteLine("\n📋 [2/3] 명령어 마스터 데이터는 Registry 기반으로 전환되었습니다.");

    // 4. 통합 명령어 보정
    Console.WriteLine("\n🧩 [3/3] 통합 명령어(UnifiedCommands) 정합성 전수 보정 중...");

    var profiles = await db.StreamerProfiles.IgnoreQueryFilters().ToListAsync();
    int provisionCount = 0;

    foreach (var p in profiles) {
        provisionCount += await EnsureCommand(db, p, "!신청", "Cheese", 1000, "SongRequest", null, CommandRole.Viewer);
        provisionCount += await EnsureCommand(db, p, "!송리스트", "None", 0, "SonglistToggle", "송리스트가 $(송리스트상태)되었습니다. ✨", CommandRole.Manager);
        
        // [매니저 전용 명령어 추가]
        provisionCount += await EnsureCommand(db, p, "!공지", "None", 0, "Notice", "공지사항: $(내용)", CommandRole.Manager);
        provisionCount += await EnsureCommand(db, p, "!방제", "None", 0, "Title", "방송 제목이 변경되었습니다: $(내용)", CommandRole.Manager);
        provisionCount += await EnsureCommand(db, p, "!카테고리", "None", 0, "Category", "카테고리가 변경되었습니다: $(내용)", CommandRole.Manager);
    }

    if (provisionCount > 0) {
        await db.SaveChangesAsync();
        Console.WriteLine($"   ✅ 명령어 {provisionCount}개 보정 완료.");
    } else {
        Console.WriteLine("   ℹ️ 모든 명령어가 정상입니다.");
    }

    // 6. [NEW] v4.9 Philosophy² & Resilience Engine 정규화 보정
    Console.WriteLine("\n🌌 [5/5] v4.9 Philosophy² & Resilience Engine 정규화 고도화 중...");
    
    // 6-1. 관리자 프로필(ID:1) 확보
    var adminProfile = await db.StreamerProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == 1);
    if (adminProfile == null)
    {
        adminProfile = new StreamerProfile 
        { 
            Id = 1, 
            ChzzkUid = configuration["MASTER_UID"] ?? "SYSTEM_ADMIN", 
            ChannelName = "SystemAdmin",
            IsDeleted = false,
            IsMasterEnabled = true
        };
        db.StreamerProfiles.Add(adminProfile);
        Console.WriteLine("   + [Admin] 관리자 프로필(ID:1)이 생성되었습니다.");
        await db.SaveChangesAsync();
    }

    // 6-2. 기존 IAMF 데이터 이관 (StreamerBound 전환)
    int migratedParhos = await db.IamfParhosCycles.Where(c => c.StreamerProfileId == 0).ExecuteUpdateAsync(s => s.SetProperty(c => c.StreamerProfileId, 1));
    int migratedGenos = await db.IamfGenosRegistries.Where(g => g.StreamerProfileId == 0).ExecuteUpdateAsync(s => s.SetProperty(g => g.StreamerProfileId, 1));
    int migratedScenarios = await db.IamfScenarios.Where(s => s.StreamerProfileId == 0).ExecuteUpdateAsync(s => s.SetProperty(sc => sc.StreamerProfileId, 1));

    if (migratedParhos > 0 || migratedGenos > 0 || migratedScenarios > 0)
        Console.WriteLine($"   ✅ IAMF 데이터 이관 완료: Parhos({migratedParhos}), Genos({migratedGenos}), Scenario({migratedScenarios})");

    // 6-3. StreamerProfile 복구 엔진용 플래그 초기화
    // [v6.2] 레거시 DelYn, MasterUseYn은 이미 삭제됨
    int flagInits = await db.StreamerProfiles.Where(p => !p.IsMasterEnabled) // 비활성화된 것들만 대상 (예시)
                                             .ExecuteUpdateAsync(s => s
                                                .SetProperty(p => p.IsDeleted, false)
                                                .SetProperty(p => p.IsMasterEnabled, true));
    
    if (flagInits > 0)
        Console.WriteLine($"   ✅ 스트리머 프로필 상태 플래그 {flagInits}개 초기화 완료.");

    Console.WriteLine("\n🎉 [완료] v4.9 데이터베이스 정문화가 성공적으로 끝났습니다!");
}
catch (Exception ex) {
    Console.WriteLine($"\n❌ [오류]: {ex.Message}");
    if (ex.InnerException != null) Console.WriteLine($"   내부: {ex.InnerException.Message}");
}

async Task<int> EnsureCommand(AppDbContext db, StreamerProfile streamer, string kw, string ct, int cost, string feature, string? response, CommandRole role)
{
    if (string.IsNullOrEmpty(kw)) return 0;
    var keywords = kw.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k));
    
    // [v4.3] 마스터 기능 레지스트리 매핑
    var masterFeature = CommandFeatureRegistry.GetByTypeName(feature);
    if (masterFeature == null) return 0;

    int added = 0;
    foreach (var k in keywords) {
        if (!await db.UnifiedCommands.IgnoreQueryFilters().AnyAsync(u => u.StreamerProfileId == streamer.Id && u.Keyword == k)) {
            db.UnifiedCommands.Add(new UnifiedCommand {
                StreamerProfileId = streamer.Id, 
                Keyword = k, 
                FeatureType = masterFeature.Type,
                CostType = Enum.Parse<CommandCostType>(ct), 
                Cost = cost, 
                ResponseText = response ?? "", 
                IsActive = true, 
                RequiredRole = role, 
                CreatedAt = KstClock.Now
            });
            added++;
        }
    }
    return added;
}

// 🔐 [오시리스의 세션]: CLI 전용 가상 사용자 세션
public class SystemUserSession : IUserSession
{
    public bool IsAuthenticated => true;
    public string? ChzzkUid => "SYSTEM";
    public string? Role => "master";
    public List<string> AllowedChannelIds => new();
}
