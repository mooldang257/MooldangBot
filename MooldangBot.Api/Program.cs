using MooldangBot.Infrastructure;
using MooldangBot.Infrastructure.Extensions;
using MooldangBot.ChzzkAPI.Clients;
using MooldangBot.ChzzkAPI.Sharding;
using MooldangBot.Application.Models.Chzzk;
using MooldangBot.Application;
using MooldangBot.Api.Middleware;
using MooldangBot.Presentation;
using MooldangBot.Presentation.Hubs;
using Microsoft.OpenApi;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Infrastructure.Security;
using MooldangBot.Presentation.Security;
using MooldangBot.Presentation.Extensions;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Services.Auth;
using MooldangBot.Application.Common.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using MooldangBot.Application.State;
using MooldangBot.Domain.Entities;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Api.Health;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Prometheus;

// [오시리스의 인장]: 애플리케이션 수명 주기 동안 로깅 보장
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try 
    {
        var builder = WebApplication.CreateBuilder(args);

    // ⚖️ [오시리스의 저울]: .env 로드 및 필수 설정값 검증 (Fail-Fast)
    builder.Configuration.AddCustomDotEnv(args).AddEnvironmentVariables();
    builder.Configuration.ValidateMandatorySecrets();

    // 🛡️ [오시리스의 확인]: 필수 연결 문자열 최종 확인 (이미 저울에서 검증됨)

    builder.Host.UseSerilog((context, services, configuration) => {
        var lokiUrl = context.Configuration["LOKI_URL"] ?? "http://localhost:3100";
        var instanceId = context.Configuration["INSTANCE_ID"] ?? "osiris-unknown";
        var env = context.HostingEnvironment.EnvironmentName;
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName() // [v16.0] 함대 위치 각인
            .Enrich.WithProperty("InstanceId", instanceId) // [v16.0] 인스턴스 식별
            .Enrich.WithProperty("Environment", env)
            .Enrich.WithProperty("Version", version)
            .WriteTo.Console()
            .WriteTo.File("logs/mooldangbot-.log", rollingInterval: RollingInterval.Day)
            .WriteTo.GrafanaLoki(lokiUrl, new[] 
            { 
                new LokiLabel { Key = "app", Value = "mooldangbot" },
                new LokiLabel { Key = "instance", Value = instanceId },
                new LokiLabel { Key = "machine", Value = Environment.MachineName },
                new LokiLabel { Key = "env", Value = env },
                new LokiLabel { Key = "version", Value = version }
            });
    });

    builder.Services.AddInfrastructureServices(builder.Configuration);

    // [v2.4.5] 치지직 전문가(Implementation) 수동 등록 (순환 참조 방지 및 전문가 보존)
    builder.Services.AddHttpClient<IChzzkApiClient, ChzzkApiClient>()
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Exception is HttpRequestException ||
                (args.Outcome.Result != null && 
                    ((int)args.Outcome.Result.StatusCode == 429 || (int)args.Outcome.Result.StatusCode >= 500))
            );
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.FailureRatio = 0.3;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result != null && 
                    ((int)args.Outcome.Result.StatusCode == 429 || (int)args.Outcome.Result.StatusCode >= 500)
            );
        });
    builder.Services.AddSingleton<IChzzkChatClient, ShardedWebSocketManager>();

    // [v2.4.8] 중복된 DataProtection 및 MemoryCache 등록을 제거하고 Infrastructure의 공통 설정을 따릅니다.

    builder.Services.AddApplicationServices();
    builder.Services.AddWebApiWorkers(); // [v2.0] API 전용 워커 등록 (Roulette, Zeroing 등)
    builder.Services.AddRabbitMqConsumer(); // [v2.0] 봇 엔진 이벤트 수신을 위한 컨슈머 등록
    builder.Services.AddPresentationServices();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserSession, UserSession>();

    builder.Services.AddMediatR(cfg => {
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        cfg.RegisterServicesFromAssembly(typeof(MooldangBot.Application.DependencyInjection).Assembly);
    });
    builder.Services.AddSingleton<SongQueueState>();
    builder.Services.AddScoped<IAuthorizationHandler, ChannelManagerAuthorizationHandler>();

    var redisUrl = builder.Configuration["REDIS_URL"]!; // [v22.0] ValidateMandatorySecrets에 의해 보장됨
    builder.Services.AddSignalR(options => {
        options.KeepAliveInterval = TimeSpan.FromSeconds(10);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(20);
    })
        .AddStackExchangeRedis(redisUrl, options => {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("MooldangBot");
        })
        .AddJsonProtocol(options => {
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
        });

    // [오시리스의 시동]: 검증된 연결 문자열을 사용하여 가동을 준비합니다.
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

    builder.Services.AddStackExchangeRedisCache(options => {
        options.Configuration = redisUrl;
        options.InstanceName = "MooldangBot_";
    });

    builder.Services.AddHealthChecks().AddCheck<BotHealthCheck>("MooldangBot_Shards");
    builder.Services.ConfigureHttpJsonOptions(options => {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
    });

    // [하모니의 조율]: API 속도 제한 서비스 등록
    builder.Services.AddMooldangRateLimiter();

    builder.Services.AddCors(options => {
        options.AddPolicy("IamfOverlayPolicy", policy => {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
    });

    builder.Services.AddAuthorization(options => {
        options.AddPolicy("ChannelManagerOnly", policy => 
            policy.Requirements.Add(new ChannelManagerRequirement()));
    });

    builder.Services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "Chzzk";
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {
        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/auth/access-denied";
        options.Cookie.Name = "MooldangBot.Auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => {
        var jwtKey = builder.Configuration["JWT_KEY"]!;
        var jwtIssuer = builder.Configuration["JWT_ISSUER"]!;
        var jwtAudience = builder.Configuration["JWT_AUDIENCE"]!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    })
    .AddChzzk(options => {
        options.ClientId = builder.Configuration["CHZZK_CLIENT_ID"]!;
        options.ClientSecret = builder.Configuration["CHZZK_CLIENT_SECRET"]!;
        options.CallbackPath = "/signin-chzzk";
        options.SaveTokens = true;

        options.Events.OnCreatingTicket = async context => {
            var chzzkUid = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(chzzkUid)) return;

            using var scope = context.HttpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);

            if (profile == null)
            {
                profile = new StreamerProfile
                {
                    ChzzkUid = chzzkUid,
                    ChannelName = context.Principal?.FindFirstValue(ClaimTypes.Name) ?? "Unknown",
                    ProfileImageUrl = context.Principal?.FindFirstValue("profile_image") ?? "",
                    Slug = chzzkUid.Substring(0, 8),
                    IsActive = true
                };
                db.StreamerProfiles.Add(profile);
            }
            else
            {
                profile.ChannelName = context.Principal?.FindFirstValue(ClaimTypes.Name) ?? profile.ChannelName;
                profile.ProfileImageUrl = context.Principal?.FindFirstValue("profile_image") ?? profile.ProfileImageUrl;
            }

            profile.ChzzkAccessToken = context.AccessToken ?? "";
            profile.ChzzkRefreshToken = context.RefreshToken ?? "";
            profile.TokenExpiresAt = DateTime.UtcNow.Add(context.ExpiresIn ?? TimeSpan.FromHours(1));

            await db.SaveChangesAsync();
        };
    });

    builder.Services.AddControllersWithViews()
        .AddJsonOptions(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddApiVersioning(options => {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options => {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddSwaggerGen(options => {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MooldangBot API", Version = "v1" });
    });

    var app = builder.Build();

    // [전역 미들웨어 설정]
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else 
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("IamfOverlayPolicy");

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.MapControllers();
    app.MapHub<OverlayHub>("/hubs/overlay");
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                shards = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description })
            });
            await context.Response.WriteAsync(result);
        }
    });

    // [오시리스의 시동]: DB 마이그레이션 및 초기화
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
    }

    Log.Information("🚀 MooldangBot API 함대가 가동되었습니다.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "🚨 MooldangBot API 기동 중 치명적인 결함이 발견되었습니다.");
}
finally
{
    Log.CloseAndFlush();
}
