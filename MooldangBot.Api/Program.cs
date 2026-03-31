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
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Api.Health;
using Serilog;
using MooldangBot.Infrastructure.Services.Serialization;

try 
{
    // 1. [Zero-Git] 실행 인자에서 설정 파일 경로 추출 (--env=.env.prod 등)
    var envPath = args.FirstOrDefault(a => a.StartsWith("--env="))?.Split('=')[1] ?? ".env";

    // 2. [파로스의 자각]: 서버 로컬에 있는 설정 파일 로드
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
            
            // [방어적 고도화]: 값 양 끝의 따옴표 제거
            if (val.Length >= 2 && ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'"))))
            {
                val = val.Substring(1, val.Length - 2);
            }
            
            var mappedKey = key.Replace("__", ":");
            builder.Configuration[mappedKey] = val;
            
            var pascalKey = string.Join(":", mappedKey.Split(':').Select(section => 
                string.Join("", section.Split('_', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1).ToLower() : p))));
            
            if (pascalKey != mappedKey)
            {
                builder.Configuration[pascalKey] = val;
            }
            
            System.Environment.SetEnvironmentVariable(key, val);
        }
    }

    // 🛡️ [오시리스의 확인]: 필수 연결 문자열 최종 검증
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connStr))
    {
        var rawConn = builder.Configuration["CONNECTIONSTRINGS:DEFAULT_CONNECTION"] 
                    ?? builder.Configuration["CONNECTIONSTRINGS__DEFAULT_CONNECTION"]
                    ?? builder.Configuration["DefaultConnection"];
        
        if (!string.IsNullOrEmpty(rawConn)) 
        {
            builder.Configuration["ConnectionStrings:DefaultConnection"] = rawConn;
            connStr = rawConn;
        }
    }

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/mooldangbot-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApplicationServices();
    builder.Services.AddPresentationServices();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserSession, UserSession>();

    builder.Services.AddMediatR(cfg => {
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        cfg.RegisterServicesFromAssembly(typeof(MooldangBot.Application.DependencyInjection).Assembly);
    });
    builder.Services.AddSingleton<SongQueueState>();
    builder.Services.AddSingleton<RouletteState>();
    builder.Services.AddHostedService<MooldangBot.Application.Workers.RouletteResultWorker>();
    builder.Services.AddScoped<IAuthorizationHandler, ChannelManagerAuthorizationHandler>();

    var redisUrl = builder.Configuration["REDIS_URL"] ?? "localhost:6379";
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(redisUrl, options => {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("MooldangBot");
        })
        .AddJsonProtocol(options => {
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
        });

    builder.Services.AddStackExchangeRedisCache(options => {
        options.Configuration = redisUrl;
        options.InstanceName = "MooldangBot_";
    });
    builder.Services.AddMemoryCache();

    builder.Services.AddHealthChecks().AddCheck<BotHealthCheck>("MooldangBot_Shards");
    builder.Services.ConfigureHttpJsonOptions(options => {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
    });

    builder.Services.AddCors(options => {
        options.AddPolicy("IamfOverlayPolicy", policy => {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
    });

    builder.Services.AddControllers().AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
    });

    builder.Services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options => { 
        options.LoginPath = "/api/auth/chzzk-login"; 
        options.Cookie.Name = builder.Configuration["AUTH_COOKIE_NAME"] ?? ".MooldangBot.Session";
    });

    builder.Services.AddAuthorization(options => {
        options.AddPolicy("ChannelManager", policy => {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new ChannelManagerRequirement());
        });
    });

    var app = builder.Build();
    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseCors("IamfOverlayPolicy");
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/", () => Results.Redirect("/bot"));
    app.MapControllers();
    app.MapHub<OverlayHub>("/overlayHub");

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        void EnsureSetting(string key, string? val) {
            if (string.IsNullOrEmpty(val)) return;
            var setting = db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
            if (setting == null) db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = val });
            else setting.KeyValue = val;
        }

        EnsureSetting("ChzzkClientId", config["CHZZK_API:CLIENT_ID"] ?? config["ChzzkApi:ClientId"]);
        EnsureSetting("ChzzkClientSecret", config["CHZZK_API:CLIENT_SECRET"] ?? config["ChzzkApi:ClientSecret"]);
        db.SaveChanges();
    }

    using (var scope = app.Services.CreateScope())
    {
        var chatClient = scope.ServiceProvider.GetRequiredService<IChzzkChatClient>();
        await chatClient.InitializeAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("\n🔥 [심각한 오류 발생]: 기동 중 예외가 발생했습니다.");
    Console.WriteLine($"   타입: {ex.GetType().Name}");
    Console.WriteLine($"   메시지: {ex.Message}");
    Console.WriteLine($"   스택 트레이스:\n{ex.StackTrace}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   내부 예외: {ex.InnerException.Message}");
    }
    throw;
}
