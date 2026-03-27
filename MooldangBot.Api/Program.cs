using MooldangBot.Infrastructure;
using MooldangBot.Application;
using MooldangBot.Presentation;
using MooldangBot.Presentation.Hubs;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Infrastructure.Security;
using MooldangBot.Presentation.Security;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.State;
using MooldangBot.Domain.Entities;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Infrastructure.Security;

// 1. [Zero-Git] 실행 인자에서 설정 파일 경로 추출 (--env=.env.prod 등)
var envPath = args.FirstOrDefault(a => a.StartsWith("--env="))?.Split('=')[1] ?? ".env";

// 2. 서버 로컬에 있는 설정 파일 로드 (Git 관리 대상 제외)
if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), envPath)))
{
    Env.Load(envPath);
    // 💡 .env 파일 내부의 ASPNETCORE_ENVIRONMENT 값을 실제 프로세스 환경 변수에 반영
    var envName = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", envName);

    // 💡 [Smart Mapping] 단일 .env 내에서 환경별 접두사(DEV_, PROD_)가 붙은 변수를 실제 변수로 자동 매핑
    var prefix = envName.ToUpper().Replace("DEVELOPMENT", "DEV") + "_";
    var allVars = System.Environment.GetEnvironmentVariables();
    foreach (string key in allVars.Keys)
    {
        if (key.StartsWith(prefix))
        {
            var actualKey = key.Substring(prefix.Length);
            var val = allVars[key]?.ToString();
            if (!string.IsNullOrEmpty(val))
            {
                System.Environment.SetEnvironmentVariable(actualKey, val);
            }
        }
    }
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(); // 시스템 환경 변수 통합

// 3. [Cloudflare Tunnel] 리버스 프록시 대응 설정 강화
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                               ForwardedHeaders.XForwardedProto | 
                               ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ==========================================
// 1. 5계층 아키텍처 기반 의존성 주입 (Phase 2 & 3)
// ==========================================
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddPresentationServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserSession, UserSession>(); // Infrastructure/Security

// -- Event-Driven Architecture (TODO: Move to Application) --
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddSingleton<SongQueueState>();
builder.Services.AddSingleton<RouletteState>();

// 🛡️ 보안 및 권한 설정 등록
builder.Services.AddScoped<IAuthorizationHandler, ChannelManagerAuthorizationHandler>();

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddMemoryCache();

// [성벽의 설계]: IAMF 오버레이 전용 CORS 정책
builder.Services.AddCors(options =>
{
    options.AddPolicy("IamfOverlayPolicy", policy =>
    {
        // 💡 [실전 배포 권장]: AllowAnyOrigin() 대신 실제 오버레이 배포 도메인을 명시하세요.
        // policy.WithOrigins("https://your-overlay-domain.vercel.app")
        policy.AllowAnyOrigin() 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// ==========================================
// 2. 인증(Authentication) 설정
// ==========================================
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options => { 
    options.LoginPath = "/api/auth/chzzk-login"; 
    options.AccessDeniedPath = "/bot";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // 로스트 테스트 대응 (HTTPS 미필수)
    
    // 5. [Multi-Instance] 인스턴스별 고유 쿠키 이름 적용
    options.Cookie.Name = builder.Configuration["AUTH_COOKIE_NAME"] ?? ".MooldangBot.Session";
    
    // AJAX 요청인 경우 302 리다이렉트 대신 401 Unauthorized 반환
    options.Events.OnRedirectToLogin = context => {
        if (context.Request.Path.StartsWithSegments("/api")) {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        } else {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ChannelManager", policy =>
    {
        policy.RequireAuthenticatedUser(); // 🛡️ 익명 사용자는 정책 검사 전 401 Unauthorized 유도
        policy.Requirements.Add(new ChannelManagerRequirement());
    });
});
var app = builder.Build();

// ==========================================
// ⭐ [추가] 프록시(Cloudflare, Nginx) 대응 설정
// ==========================================
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // 💡 클라우드플레어 터널 등 프록시 환경에서 프로토콜(HTTPS) 정보를 정확히 읽어오도록 신뢰 설정을 추가합니다.
    KnownIPNetworks = { },
    KnownProxies = { },
    ForwardLimit = null // 프록시 제한을 풀어서 모든 홉을 신뢰하게 함
});

app.UseWebSockets();

// 6. [Cloudflare] Forwarded Headers 미들웨어 (인증/라우팅 전 필수)
app.UseForwardedHeaders();

app.UseRouting();
app.UseCors("IamfOverlayPolicy");

// 미들웨어 설정
app.UseStaticFiles();
app.UseAuthentication();

// 🔐 [추가] 인증된 세션에 StreamerId(ChzzkUid)가 반드시 포함되어 있는지 검증하는 미들웨어
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var streamerId = context.User.FindFirstValue("StreamerId");
        if (string.IsNullOrEmpty(streamerId))
        {
            // 인증은 되었으나 식별값이 없는 비정상 세션인 경우 로그아웃 처리 후 로그인 페이지로 유도
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/api/auth/chzzk-login");
            return;
        }
    }
    await next();
});

app.UseAuthorization();


// ==========================================
// 3. 🌐 화면 라우팅 (View)
// ==========================================
app.MapGet("/", () => Results.Redirect("/bot"));



// ==========================================
// 4. 🔐 치지직 공식 인증 (OAuth)
// ==========================================

app.MapControllers();




app.MapHub<OverlayHub>("/overlayHub");

// 8443 포트 사용을 위해 ASPNETCORE_URLS 환경변수를 쓰거나 아래를 수정하세요.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // db.Database.Migrate(); // [수동 관리] 기존 테이블 충돌 방지를 위해 자동 마이그레이션 비활성화DB가 초기화되었을 때 appsettings의 값을 DB에 자동으로 채워줍니다.
    // 💡 [DB 초기값 세팅] 리눅스 도커 환경에서 DB가 초기화되었을 때 appsettings의 값을 DB에 자동으로 채워줍니다.
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    void EnsureSetting(string key, string? val)
    {
        if (string.IsNullOrEmpty(val)) return;
        var setting = db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
        if (setting == null)
        {
            db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = val });
        }
        else
        {
            // [강제 업데이트] appsettings.json의 최신값을 DB에 반영
            setting.KeyValue = val;
        }
    }

    EnsureSetting("ChzzkClientId", config["ChzzkApi:ClientId"]);
    EnsureSetting("ChzzkClientSecret", config["ChzzkApi:ClientSecret"]);
    db.SaveChanges();

    // 🏗️ [DB 스키마 유지보수] SongBook 테이블이 없으면 자동 생성 (수동 관리 환경 대응)
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS `songbooks` (
            `Id` INT NOT NULL AUTO_INCREMENT,
            `ChzzkUid` VARCHAR(50) NOT NULL,
            `Title` VARCHAR(200) NOT NULL,
            `Artist` VARCHAR(100) NULL,
            `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
            `UsageCount` INT NOT NULL DEFAULT 0,
            `CreatedAt` DATETIME(6) NOT NULL,
            `UpdatedAt` DATETIME(6) NOT NULL,
            PRIMARY KEY (`Id`),
            INDEX `IX_songbooks_ChzzkUid_Id` (`ChzzkUid` ASC, `Id` DESC)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS `roulettelogs` (
            `Id` BIGINT NOT NULL AUTO_INCREMENT,
            `ChzzkUid` VARCHAR(100) NOT NULL,
            `ViewerNickname` VARCHAR(100) NOT NULL,
            `ItemName` VARCHAR(200) NOT NULL,
            `IsMission` TINYINT(1) NOT NULL,
            `Status` INT NOT NULL,
            `CreatedAt` DATETIME(6) NOT NULL,
            `ProcessedAt` DATETIME(6) NULL,
            PRIMARY KEY (`Id`),
            INDEX `IX_roulettelogs_ChzzkUid_Status_Id` (`ChzzkUid` ASC, `Status` ASC, `Id` DESC)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
    ");

    // 🏗️ [DB 스키마 유지보수] 기존 rouletteitems 테이블에 IsMission 컬럼 추가 (v5 대응)
    try
    {
        // MySQL/MariaDB 버전에 따라 IF NOT EXISTS 지원 여부가 다를 수 있으므로 try-catch로 안전하게 처리
        db.Database.ExecuteSqlRaw(@"
            ALTER TABLE `rouletteitems` ADD COLUMN `IsMission` TINYINT(1) NOT NULL DEFAULT 0;
        ");
    }
    catch (Exception ex)
    {
        // 이미 컬럼이 존재하는 경우(Duplicate column error) 무시하고 진행
        Console.WriteLine($"[DB 체크] IsMission 컬럼 확인 중: {ex.Message}");
    }
}

app.Run();

