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
using MooldangBot.Application.Interfaces;
using DotNetEnv;

string Mask(string s) => s.Length > 4 ? s[..4] + "****" : "****";

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
    
    var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    var prefix = (envName.ToUpper().Replace("DEVELOPMENT", "DEV").Replace("PRODUCTION", "PROD")) + "_";
    
    foreach (var line in File.ReadAllLines(foundPath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
        var split = trimmed.Split('=', 2);
        if (split.Length != 2) continue;
        var key = split[0].Trim();
        var val = split[1].Trim();
        
        overrides[key.Replace("__", ":")] = val;
        if (key.StartsWith(prefix))
        {
            var actualKey = key.Substring(prefix.Length).Replace("__", ":");
            overrides[actualKey] = val;
            if (actualKey.StartsWith("CONNECTIONSTRINGS:", StringComparison.OrdinalIgnoreCase))
                overrides["ConnectionStrings:" + actualKey.Substring(18)] = val;
        }
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

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());

// 🔐 CLI용 시스템 세션 등록 (DbContext 의존성 해결)
services.AddSingleton<IUserSession, SystemUserSession>();

services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Parse("10.11-mariadb")));

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

    // 3. 시스템 설정 시딩
    Console.WriteLine("\n🌱 [2/3] 필수 시스템 설정(SystemSettings) 동기화 중...");
    async Task SyncSetting(string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var existing = await db.SystemSettings.FindAsync(key);
        if (existing == null) {
            db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
            Console.WriteLine($"   + [{key}] 생성됨: {Mask(value)}");
        } else if (string.IsNullOrEmpty(existing.KeyValue)) {
            existing.KeyValue = value;
            Console.WriteLine($"   * [{key}] 업데이트됨: {Mask(value)}");
        } else {
            Console.WriteLine($"   - [{key}] 유지됨.");
        }
    }

    await SyncSetting("ChzzkClientId", configuration["CHZZK_API:CLIENT_ID"] ?? configuration["ChzzkApi:ClientId"]);
    await SyncSetting("ChzzkClientSecret", configuration["CHZZK_API:CLIENT_SECRET"] ?? configuration["ChzzkApi:ClientSecret"]);
    await SyncSetting("BaseDomain", configuration["BASE_DOMAIN"]);
    await SyncSetting("MasterUid", configuration["MASTER_UID"]);
    await db.SaveChangesAsync();

    // 4. [NEW] 마스터 데이터 시딩
    Console.WriteLine("\n📋 [3/4] 명령어 마스터 데이터(Categories/Features) 동기화 중...");
    async Task SyncCategory(string name, string display, int order) {
        var existing = await db.UnifiedCommandCategories.FindAsync(name);
        if (existing == null) {
            db.UnifiedCommandCategories.Add(new UnifiedCommandCategory { CategoryName = name, DisplayName = display, SortOrder = order });
            Console.WriteLine($"   + [Category] {name} 생성됨");
        }
    }
    await SyncCategory("General", "일반 (채팅/공지)", 1);
    await SyncCategory("Fixed", "시스템 고정 (출석 등)", 2);
    await SyncCategory("Donation", "후원 연동 (곡/룰렛)", 3);
    await SyncCategory("Point", "포인트 소모 (룰렛 등)", 4);

    async Task SyncFeature(string cat, string name, string display, string? resp, int cost, string costType, CommandRole role) {
        var existing = await db.UnifiedCommandFeatures.FirstOrDefaultAsync(f => f.CategoryName == cat && f.FeatureName == name);
        if (existing == null) {
            db.UnifiedCommandFeatures.Add(new UnifiedCommandFeature { 
                CategoryName = cat, FeatureName = name, DisplayName = display, 
                DefaultResponse = resp, DefaultCost = cost, DefaultCostType = costType, DefaultRequiredRole = role 
            });
            Console.WriteLine($"   + [Feature] {cat}/{name} 생성됨");
        }
    }
    await SyncFeature("General", "Reply", "💬 채팅 답변", null, 0, "None", CommandRole.Viewer);
    await SyncFeature("General", "Notice", "📢 상단 공지", "공지사항: {내용}", 0, "None", CommandRole.Manager);
    await SyncFeature("General", "SonglistToggle", "🔒 송리스트 ON/OFF", "송리스트가 {송리스트상태}되었습니다. ✨", 0, "None", CommandRole.Manager);
    await SyncFeature("General", "Title", "📝 방송 제목 변경", "방송 제목이 변경되었습니다: {내용}", 0, "None", CommandRole.Manager);
    await SyncFeature("General", "Category", "🎮 카테고리 변경", "카테고리가 변경되었습니다: {내용}", 0, "None", CommandRole.Manager);
    await SyncFeature("Fixed", "Attendance", "✅ 출석체크", "{닉네임}님 출석 고마워요!", 0, "None", CommandRole.Viewer);
    await SyncFeature("Fixed", "PointCheck", "💰 포인트 조회", "🪙 {닉네임}님의 보유 포인트는 {포인트}점입니다!", 0, "None", CommandRole.Viewer);
    await SyncFeature("Donation", "SongRequest", "🎵 노래 신청", null, 1000, "Cheese", CommandRole.Viewer);
    await SyncFeature("Donation", "Roulette", "🎰 후원 룰렛", null, 0, "Cheese", CommandRole.Viewer);
    await SyncFeature("Point", "Roulette", "🎰 포인트 룰렛", null, 0, "Point", CommandRole.Viewer);
    await db.SaveChangesAsync();

    // 5. 통합 명령어 보정
    Console.WriteLine("\n🧩 [4/4] 통합 명령어(UnifiedCommands) 정합성 전수 보정 중...");
    var profiles = await db.StreamerProfiles.IgnoreQueryFilters().ToListAsync();
    int provisionCount = 0;
    foreach (var p in profiles) {
        provisionCount += await EnsureCommand(db, p.ChzzkUid, p.SongCommand ?? "!신청", "Donation", "Cheese", 1000, "SongRequest", null, CommandRole.Viewer);
        provisionCount += await EnsureCommand(db, p.ChzzkUid, "!송리스트", "General", "None", 0, "SonglistToggle", "송리스트가 {송리스트상태}되었습니다. ✨", CommandRole.Manager);
        
        // [매니저 전용 명령어 추가]
        provisionCount += await EnsureCommand(db, p.ChzzkUid, "!공지", "General", "None", 0, "Notice", "공지사항: {내용}", CommandRole.Manager);
        provisionCount += await EnsureCommand(db, p.ChzzkUid, "!방제", "General", "None", 0, "Title", "방송 제목이 변경되었습니다: {내용}", CommandRole.Manager);
        provisionCount += await EnsureCommand(db, p.ChzzkUid, "!카테고리", "General", "None", 0, "Category", "카테고리가 변경되었습니다: {내용}", CommandRole.Manager);
    }

    if (provisionCount > 0) {
        await db.SaveChangesAsync();
        Console.WriteLine($"   ✅ 명령어 {provisionCount}개 보정 완료.");
    } else {
        Console.WriteLine("   ℹ️ 모든 명령어가 정상입니다.");
    }

    Console.WriteLine("\n🎉 [완료] 데이터베이스 정문화가 성공적으로 끝났습니다!");
}
catch (Exception ex) {
    Console.WriteLine($"\n❌ [오류]: {ex.Message}");
    if (ex.InnerException != null) Console.WriteLine($"   내부: {ex.InnerException.Message}");
}

async Task<int> EnsureCommand(AppDbContext db, string uid, string kw, string cat, string ct, int cost, string feature, string? response, CommandRole role)
{
    if (string.IsNullOrEmpty(kw)) return 0;
    var keywords = kw.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k));
    int added = 0;
    foreach (var k in keywords) {
        if (!await db.UnifiedCommands.IgnoreQueryFilters().AnyAsync(u => u.ChzzkUid == uid && u.Keyword == k)) {
            db.UnifiedCommands.Add(new UnifiedCommand {
                ChzzkUid = uid, Keyword = k, Category = Enum.Parse<CommandCategory>(cat),
                CostType = Enum.Parse<CommandCostType>(ct), Cost = cost, FeatureType = feature,
                ResponseText = response ?? "", IsActive = true, RequiredRole = role, UpdatedAt = DateTime.Now
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
