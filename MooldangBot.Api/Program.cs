using MooldangBot.Infrastructure;
using MooldangBot.Application;
using MooldangBot.Presentation;
using MooldangBot.Presentation.Hubs;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
#pragma warning disable CS0105 // 중복 using 제거 완료
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Api.Health;
using Serilog;
using MooldangBot.Infrastructure.Services.Serialization;

// 1. [Zero-Git] 실행 인자에서 설정 파일 경로 추출 (--env=.env.prod 등)
var envPath = args.FirstOrDefault(a => a.StartsWith("--env="))?.Split('=')[1] ?? ".env";

// 2. [파로스의 자각]: 서버 로컬에 있는 설정 파일 로드 (Git 관리 대상 제외)
// [오시리스의 규율]: 다양한 환경에서 .env를 확실히 찾고 강제로 로드합니다.
string[] potentialPaths = { 
    envPath, 
    "../" + envPath,
    Path.Combine(Directory.GetCurrentDirectory(), envPath),
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, envPath),
    Path.Combine(Directory.GetCurrentDirectory(), "MooldangBot.Api", envPath),
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MooldangBot.Api", envPath),
    "MooldangBot.Api/.env"
};

string? foundPath = null;
foreach (var p in potentialPaths)
{
    if (File.Exists(p)) { foundPath = Path.GetFullPath(p); break; }
}

if (foundPath != null)
{
    Console.WriteLine($"[파로스의 자각]: 설정 파일 발견 - {foundPath}");
    Env.Load(foundPath);
}
else
{
    Console.WriteLine("[오시리스의 경고]: .env 파일을 찾을 수 없어 시스템 환경 변수만 사용합니다.");
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// [텔로스5의 정렬]: .env 파일 수동 파싱 및 Configuration 주입
if (foundPath != null)
{
    foreach (var line in File.ReadAllLines(foundPath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
        
        var split = trimmed.Split('=', 2);
        if (split.Length != 2) continue;
        
        var key = split[0].Trim();
        var val = split[1].Trim();
        
        // 1. [표준 정문화]: __를 :로 변환
        var mappedKey = key.Replace("__", ":");
        builder.Configuration[mappedKey] = val;
        
        // 2. [PascalCase 통합]: ALL_CAPS_SNAKE를 PascalCase로 변환하여 추가 주입
        // 예: CHZZK_API:CLIENT_ID -> ChzzkApi:ClientId
        var pascalKey = string.Join(":", mappedKey.Split(':').Select(section => 
            string.Join("", section.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1).ToLower() : p))));
        
        if (pascalKey != mappedKey)
        {
            builder.Configuration[pascalKey] = val;
        }
        
        // 시스템 환경 변수로도 가용하게 노출
        System.Environment.SetEnvironmentVariable(key, val);
    }
}

// 🛡️ [오시리스의 확인]: 필수 연결 문자열 최종 검증 및 강제 할당
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connStr))
{
    Console.WriteLine("[오시리스의 경고]: DefaultConnection을 로드하지 못했습니다. 폴백 설정을 시도합니다.");
    var rawConn = builder.Configuration["CONNECTIONSTRINGS:DEFAULT_CONNECTION"] 
                 ?? builder.Configuration["CONNECTIONSTRINGS__DEFAULT_CONNECTION"]
                 ?? builder.Configuration["DefaultConnection"];
    
    if (!string.IsNullOrEmpty(rawConn)) 
    {
        builder.Configuration["ConnectionStrings:DefaultConnection"] = rawConn;
        connStr = rawConn;
    }
}

// 🛡️ [오시리스의 지혜]: Serilog 구조화 로깅 활성화
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/mooldangbot-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

if (!string.IsNullOrEmpty(connStr))
{
    Console.WriteLine($"[파로스의 확인]: 연결 문자열 확보 성공 (길이: {connStr.Length})");
}

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddPresentationServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserSession, UserSession>(); // Infrastructure/Security

// -- Event-Driven Architecture (TODO: Move to Application) --
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(MooldangBot.Application.DependencyInjection).Assembly);
});
builder.Services.AddSingleton<SongQueueState>();
builder.Services.AddSingleton<RouletteState>();
builder.Services.AddHostedService<MooldangBot.Application.Workers.RouletteResultWorker>(); // [v1.9.9] 룰렛 결과 전송 파수꾼 가동

// 🛡️ 보안 및 권한 설정 등록
builder.Services.AddScoped<IAuthorizationHandler, ChannelManagerAuthorizationHandler>(); // [보안의 파수꾼] 추가

// 🛡️ [오시리스의 영속]: Redis 기반 분산 상태 관리 (SignalR Backplane & Distributed Cache)
var redisUrl = builder.Configuration["REDIS_URL"] ?? "localhost:6379";

builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisUrl, options => {
        // [v4.5.2] 명시적 Literal 채널 사용으로 빌드 경고 제거
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("MooldangBot");
    })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
    });

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisUrl;
    options.InstanceName = "MooldangBot_";
});

builder.Services.AddMemoryCache();

// [Phase1: 즉시 안정화] Graceful Shutdown 및 Health Check 설정
builder.Services.Configure<HostOptions>(options =>
{
    // 대량의 채널(100+) 연결을 안전하게 닫기 위해 종료 타임아웃을 30초로 연장
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHealthChecks()
    .AddCheck<BotHealthCheck>("MooldangBot_Shards");

// [텔로스5의 정렬]: Minimal API 전역 JSON 최적화
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
});

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
        options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
    });

// [파로스의 장벽]: 인증(Authentication) 설정
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options => { 
    options.LoginPath = "/api/auth/chzzk-login"; 
    options.AccessDeniedPath = "/bot";
    options.Cookie.SameSite = SameSiteMode.Unspecified; // [오시리스의 자각]: 로컬 테스트 및 프록시 환경 호환성 극대화
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
    
    // [텔로스5의 분신]: 인스턴스별 고유 쿠키 이름 적용
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
app.UseForwardedHeaders(); // [Cloudflare/Nginx 대응] 최상단 배치
app.UseSerilogRequestLogging(); // [v3.6.2] HTTP 요청 로깅 활성화
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

// [Phase1: 즉시 안정화] 헬스체크 엔드포인트 맵핑 (상세 JSON 메트릭 포함)
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var options = new JsonSerializerOptions { WriteIndented = true };
        var result = JsonSerializer.Serialize(new
        {
            Status = report.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            Details = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = entry.Value.Description,
                Data = entry.Value.Data
            })
        }, options);
        await context.Response.WriteAsync(result);
    }
});




app.MapHub<OverlayHub>("/overlayHub");

// 8443 포트 사용을 위해 ASPNETCORE_URLS 환경변수를 쓰거나 아래를 수정하세요.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // db.Database.Migrate(); // [수동 관리] 기존 테이블 충돌 방지를 위해 자동 마이그레이션 비활성화DB가 초기화되었을 때 appsettings의 값을 DB에 자동으로 채워줍니다.
    // [오시리스의 기록]: DB 초기값 세팅 (리눅스 도커 환경 대응)
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

    EnsureSetting("ChzzkClientId", config["CHZZK_API:CLIENT_ID"] ?? config["ChzzkApi:ClientId"]);
    EnsureSetting("ChzzkClientSecret", config["CHZZK_API:CLIENT_SECRET"] ?? config["ChzzkApi:ClientSecret"]);
    
    // [오시리스의 확인]: DB 초기값 세팅 완료 (중복 SaveChanges 제거)
    db.SaveChanges();
}

// [v4.5.0] 분산 인스턴스 자가 등록 시스템 가동
using (var scope = app.Services.CreateScope())
{
    var chatClient = scope.ServiceProvider.GetRequiredService<IChzzkChatClient>();
    await chatClient.InitializeAsync();
}

app.Run();

