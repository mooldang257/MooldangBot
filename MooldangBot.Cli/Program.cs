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
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

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
    async Task SyncCategory(int id, string name, string display, int order) {
        var existing = await db.MasterCommandCategories.FindAsync(id);
        if (existing == null) {
            db.MasterCommandCategories.Add(new Master_CommandCategory { Id = id, Name = name, DisplayName = display, SortOrder = order });
            Console.WriteLine($"   + [Category] {name} 생성됨");
        }
    }
    await SyncCategory(1, "General", "일반", 1);
    await SyncCategory(2, "System", "시스템메세지", 2);
    await SyncCategory(3, "Feature", "기능", 3);

    async Task SyncFeature(int id, int catId, string type, string display, int cost, CommandRole role) {
        var existing = await db.MasterCommandFeatures.FindAsync(id);
        if (existing == null) {
            db.MasterCommandFeatures.Add(new Master_CommandFeature { 
                Id = id, CategoryId = catId, TypeName = type, DisplayName = display, 
                DefaultCost = cost, RequiredRole = role 
            });
            Console.WriteLine($"   + [Feature] {type} 생성됨");
        }
    }
    await SyncFeature(1, 1, "Reply", "텍스트 답변", 0, CommandRole.Viewer);
    await SyncFeature(2, 2, "Notice", "공지", 0, CommandRole.Manager);
    await SyncFeature(3, 2, "Title", "방제", 0, CommandRole.Manager);
    await SyncFeature(4, 2, "Category", "카테고리", 0, CommandRole.Manager);
    await SyncFeature(5, 2, "SonglistToggle", "송리스트", 0, CommandRole.Manager);
    await SyncFeature(6, 3, "SongRequest", "노래신청", 1000, CommandRole.Viewer);
    await SyncFeature(7, 3, "Omakase", "오마카세", 1000, CommandRole.Viewer);
    await SyncFeature(8, 3, "Roulette", "룰렛", 500, CommandRole.Viewer);
    await SyncFeature(9, 3, "ChatPoint", "채팅포인트", 0, CommandRole.Viewer);
    await SyncFeature(10, 2, "SystemResponse", "시스템 응답", 0, CommandRole.Manager);
    await SyncFeature(11, 3, "AI", "AI 답변", 1000, CommandRole.Viewer);
    await db.SaveChangesAsync();

    // 5. 통합 명령어 보정
    Console.WriteLine("\n🧩 [4/4] 통합 명령어(UnifiedCommands) 정합성 전수 보정 중...");
    var profiles = await db.StreamerProfiles.IgnoreQueryFilters().ToListAsync();
    int provisionCount = 0;
    foreach (var p in profiles) {
        provisionCount += await EnsureCommand(db, p.ChzzkUid, p.SongCommand ?? "!신청", "Feature", "Cheese", 1000, "SongRequest", null, CommandRole.Viewer);
        provisionCount += await EnsureCommand(db, p.ChzzkUid, "!송리스트", "System", "None", 0, "SonglistToggle", "송리스트가 {송리스트상태}되었습니다. ✨", CommandRole.Manager);
        
        // [매니저 전용 명령어 추가]
        provisionCount += await EnsureCommand(db, p.ChzzkUid, "!공지", "System", "None", 0, "Notice", "공지사항: {내용}", CommandRole.Manager);
        provisionCount += await EnsureCommand(db, p.ChzzkUid, "!방제", "System", "None", 0, "Title", "방송 제목이 변경되었습니다: {내용}", CommandRole.Manager);
        provisionCount += await EnsureCommand(db, p.ChzzkUid, "!카테고리", "System", "None", 0, "Category", "카테고리가 변경되었습니다: {내용}", CommandRole.Manager);
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
                ResponseText = response ?? "", IsActive = true, RequiredRole = role, CreatedAt = KstClock.Now
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
