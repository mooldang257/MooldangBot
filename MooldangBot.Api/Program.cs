using MooldangBot.Modules.Commands;
using MooldangBot.Infrastructure;
using MooldangBot.Modules.SongBookModule;
using MooldangBot.Contracts.Extensions;
using MooldangBot.Contracts.Models.Chzzk;
using System.Reflection;
using System.IO;
using MooldangBot.Application;
using MooldangBot.Modules.Roulette;
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
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.AI.Interfaces;
using MooldangBot.Contracts.Commands.Interfaces;
using MooldangBot.Contracts.Point.Interfaces;
using MooldangBot.Infrastructure.Extensions;
using MooldangBot.Application.Common.Security;
using MooldangBot.Application.Services.Auth;
using MooldangBot.Contracts.Security;
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
using MooldangBot.Contracts.Chzzk;

// [오시리스의 인장]: 애플리케이션 수명 주기 동안 로깅 보장
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try 
{
    var builder = WebApplication.CreateBuilder(args);

    // 🛡️ [오시리스의 가호]: .env 로드 및 필수 설정값 검증 (Fail-Fast)
    builder.Configuration.AddCustomDotEnv(args).AddEnvironmentVariables();
    builder.Configuration.ValidateMandatorySecrets();

    // 🔒 [오시리스의 확인]: 필수 연결 문자열 최종 확인 (이미 가호에서 검증됨)

    builder.Host.UseSerilog((context, services, configuration) => {
        var lokiUrl = context.Configuration["LOKI_URL"] ?? "http://localhost:3100";
        var instanceId = context.Configuration["INSTANCE_ID"] ?? "osiris-unknown";
        var env = context.HostingEnvironment.EnvironmentName;
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName() // [v16.0] 장비 위치 각인
            .Enrich.WithProperty("InstanceId", instanceId) // [v16.0] 인스턴스 식별
            .Enrich.WithProperty("Environment", env)
            .Enrich.WithProperty("Version", version)
            .WriteTo.Async(a => a.Console())
            .WriteTo.Async(a => a.File("logs/mooldangbot-.log", rollingInterval: RollingInterval.Day))
            .WriteTo.Async(a => a.GrafanaLoki(lokiUrl, new[] 
            { 
                new LokiLabel { Key = "app", Value = "mooldangbot" },
                new LokiLabel { Key = "instance", Value = instanceId },
                new LokiLabel { Key = "machine", Value = Environment.MachineName },
                new LokiLabel { Key = "env", Value = env },
                new LokiLabel { Key = "version", Value = version }
            }));
    });

    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddSongBookModule();
    builder.Services.AddRouletteModule();
    builder.Services.AddCommandsModule();
    builder.Services.AddMessagingInfrastructure(builder.Configuration, typeof(MooldangBot.Application.Consumers.ChatReceivedConsumer).Assembly);

    // [오시리스의 검증]: 인프라 DI 통합성 최종 확인 가드
    // 만약 인프라 서비스 등록 과정에서 누락되었다면 여기에서 강제로 계통을 연결합니다.
    if (!builder.Services.Any(x => x.ServiceType == typeof(MooldangBot.Contracts.Common.Interfaces.IChzzkChatClient)))
    {
        Log.Warning("⚠️ [DI Guard] IChzzkChatClient was missing from Infrastructure services. Forcing registration...");
        builder.Services.AddSingleton<MooldangBot.Contracts.Common.Interfaces.IChzzkChatClient, MooldangBot.Infrastructure.ApiClients.GatewayChatClientProxy>();
    }

    // [v2.4.5] 치지직 전문가 등록은 Infrastructure 레이어의 AddInfrastructureServices에서 통합 관리됩니다.
    // [중복 제거]: builder.Services.AddHttpClient<IChzzkApiClient, ChzzkApiClient>() 로직은 인프라로 이동됨
    // [v4.5.3] 채팅 클라이언트 관리 주체는 Infrastructure 레이어로 격리됨 (GatewayChatClientProxy 사용)

    builder.Services.AddApplicationServices();
    builder.Services.AddWebApiWorkers(); // [v2.0] API 전용 워커 등록 (Roulette, Zeroing 등)
    
    // [v4.5.3] 10k TPS 고밀도 대응을 위한 MassTransit 소비 파이프라인이 가동되었습니다. (브릿지 레이어 제거)
    // [v3.7] MassTransit Consumer가 모든 역할을 대체하므로 매거진 수집기는 제거되었습니다.
    
    builder.Services.AddPresentationServices();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserSession, UserSession>();

    // 🔍 [지능형 광역 소나]: MooldangBot 관련 모든 어셈블리를 감지하여 MediatR 핸들러를 자동 등록합니다. (사각지대 방지 로직 포함)
    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
    var executionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    if (executionPath != null)
    {
        foreach (var dll in Directory.GetFiles(executionPath, "MooldangBot.*.dll"))
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(dll);
                if (loadedAssemblies.All(a => a.FullName != assemblyName.FullName))
                {
                    loadedAssemblies.Add(Assembly.Load(assemblyName));
                }
            }
            catch { /* Ignore load errors */ }
        }
    }

    var finalAssemblies = loadedAssemblies
        .Where(a => a.FullName != null && a.FullName.StartsWith("MooldangBot"))
        .ToArray();

    builder.Services.AddMediatR(cfg => {
        cfg.RegisterServicesFromAssemblies(finalAssemblies);
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

    // [오시리스의 기동]: 검증된 연결 문자열을 사용하여 가동을 준비합니다.
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

        // [오시리스의 중계]: Studio 프론트엔드 연동을 위한 전용 CORS 정책
        options.AddPolicy("StudioCorsPolicy", policy => {
            policy.WithOrigins("http://localhost:3000", "https://www.mooldang.store") // 로컬 및 운영 도메인 허용
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // 최중요: 인증 쿠키(세션) 공유를 위해 필수
        });
    });

    builder.Services.AddScoped<IAuthorizationHandler, OverlayTokenVersionHandler>();

    // 🌉 [Aegis Bridge]: 프록시(Vite/Nginx) 환경에서의 원본 IP 및 프로토콜 전달 설정
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // 로컬 개발 환경(localhost)에서의 프록시는 신뢰하도록 설정
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // 🛡️ [이지스의 방패]: API 버전 관리 등록
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true; // 버전 명시 없을 시 기본 요청은 1.0으로 간주
        options.ReportApiVersions = true; // 응답 헤더에 지원하는 버전 명시
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version") // 헤더 방식도 예비로 지정
        );
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // ✨ [이지스의 정화]: FluentValidation 자동 검증 등록
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssembly(typeof(MooldangBot.Contracts.Common.Interfaces.IAppDbContext).Assembly);

    builder.Services.AddControllers()
        .AddApplicationPart(typeof(MooldangBot.Presentation.DependencyInjection).Assembly)
        .AddJsonOptions(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
        });

    // 📜 [오시리스의 선고]: Swagger/OpenAPI 상세 문서화 설정
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "MooldangBot API", 
            Version = "v1.1",
            Description = "[하모니의 조화지]: 물댕봇 백엔드 통합 API 명세서입니다."
        });

        // JWT 인증 UI 추가
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        // OpenAPI 2.x (OAS 3.x) 보다 기반 보안 요구사항 규격 적용
        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", null, null)] = new List<string>()
        });
    });

    // [v4.0] 오시리스의 검침: JWT 시크릿키 검증 (가호에서 수행됨)
    var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
    
    if (Encoding.UTF8.GetBytes(jwtSecret).Length < 32)
    {
        throw new InvalidOperationException("❌ [Security Error]: JWT 시크릿 키가 너무 짧습니다. 최소 32바이트(256비트) 이상의 키를 사용하십시오.");
    }
    var issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

    builder.Services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options => { 
        options.LoginPath = "/api/auth/chzzk-login"; 
        options.Cookie.Name = builder.Configuration["AUTH_COOKIE_NAME"] ?? ".MooldangBot.Session";
        
        // [Aegis 로컬 패스]: 로컬/HTTP 환경에서의 세션을 유지하도록 보안 정책 조정
        var isDev = builder.Environment.IsDevelopment();
        options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax; // [v22.2] 동일 도메인 환경에서의 세션 유지력 강화 (Lax 권장)
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true; // [v22.1] 필수 쿠키로 지정하여 시스템 가용성 보장
    })
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "MooldangBot",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "MooldangBot_Overlay",
            IssuerSigningKey = issuerSigningKey
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // SignalR은 쿼리 스트링("access_token")으로 토큰을 보내올 수 있으므로 이를 잡아챕니다.
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/overlayHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization(options => {
        // [오시리스의 통합 정책]: 쿠키와 JWT 인증을 모두 허용하는 기본 인가 정책
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme)
            .Build();

        options.AddPolicy("ChannelManager", policy => {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new ChannelManagerRequirement());
        });

        // [오시리스의 공명]: 오버레이 전용 (JWT 권장 + 버전 검증) 정책
        options.AddPolicy("OverlayAuth", policy => {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new OverlayTokenVersionRequirement()); // 🔍 버전 실시간 검증
        });
    });

    var app = builder.Build();

    // 1. [오시리스의 심판]: 가장 먼저 예외를 잡아채기 위해 최상단에 배치
    app.UseMiddleware<ExceptionMiddleware>();

    // 2. 헬스체크 로그 스팸 방지 (정상시에는 Verbose, 에러시에는 Error)
    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (ctx, _, ex) => 
            ex != null || ctx.Response.StatusCode > 499 ? Serilog.Events.LogEventLevel.Error :
            ctx.Request.Path.StartsWithSegments("/health") ? Serilog.Events.LogEventLevel.Verbose : 
            Serilog.Events.LogEventLevel.Information;
    });

    app.UseForwardedHeaders();
    app.UseStaticFiles(); // [하모니의 화랑]: 업로드된 이미지 등 정적 파일 서비스 활성화
    app.UseRouting();

    // 🔒 [오시리스의 전송]: 속도 제한은 라우팅 직후, CORS와 함께 배치
    app.UseRateLimiter();
    // [오시리스의 중계]: 주요 조력자(Studio)를 위한 자격 증명 허용 정책 우선 적용
    app.UseCors("StudioCorsPolicy"); 
    app.UseCors("IamfOverlayPolicy"); // [v6.2] 레거시 오버레이 호환성 유지

    app.UseAuthentication();
    app.UseAuthorization();

    // 3. ✍️ [하모니의 기록]: 반드시 인증(Authorization) 직후에 배치해야 User 정보가 존재함
    app.UseMiddleware<LogEnrichmentMiddleware>();

    // app.MapGet("/", () => Results.Redirect("/swagger")); // [v2.4.1] Prometheus 메트릭 미들웨어 및 엔드포인트 노출
    app.UseHttpMetrics();

    // ---------------------------------------------------------
    // [최후의 보루]: 애플리케이션 시작
    // ---------------------------------------------------------
    app.MapControllers();
    app.MapMetrics(); // /metrics 엔드포인트 매핑
    app.MapHealthChecks("/health"); // [오시리스의 박동]: 컨테이너 상태 감시 전용 엔드포인트
    app.MapHub<OverlayHub>("/overlayHub");

    // [v4.9] Swagger UI 활성화 (보안을 위해 개발 환경 또는 명시적 설정 시에만 노출)
    if (app.Environment.IsDevelopment() || app.Configuration["EnableSwagger"] == "true")
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MooldangBot API v1.1");
        });
    }

    // [오시리스의 시동]: 시스템 초기화 (DB 시딩 및 서비스 기동)
    using (var scope = app.Services.CreateScope())
    {
        await scope.ServiceProvider.GetRequiredService<IDbInitializer>().InitializeAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ [심각한 오류 발생]: 애플리케이션 기동 중 복구 불가능한 예외가 발생했습니다.");
    throw;
}
finally
{
    Log.Information("🕊️ [오시리스의 평온]: 애플리케이션이 안전하게 종료됩니다.");
    Log.CloseAndFlush();
}
