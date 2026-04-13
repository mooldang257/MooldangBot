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
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
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
// [?ㅼ떆由ъ뒪???몄옣]: ?좏뵆由ъ??댁뀡 ?섎챸 二쇨린 ?숈븞 濡쒓퉭 蹂댁옣
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try 
{
    var builder = WebApplication.CreateBuilder(args);

    // ?뽳툘 [?ㅼ떆由ъ뒪?????: .env 濡쒕뱶 諛??꾩닔 ?ㅼ젙媛?寃利?(Fail-Fast)
    builder.Configuration.AddCustomDotEnv(args).AddEnvironmentVariables();
    builder.Configuration.ValidateMandatorySecrets();

    // ?썳截?[?ㅼ떆由ъ뒪???뺤씤]: ?꾩닔 ?곌껐 臾몄옄??理쒖쥌 ?뺤씤 (?대? ??몄뿉??寃利앸맖)

    builder.Host.UseSerilog((context, services, configuration) => {
        var lokiUrl = context.Configuration["LOKI_URL"] ?? "http://localhost:3100";
        var instanceId = context.Configuration["INSTANCE_ID"] ?? "osiris-unknown";
        var env = context.HostingEnvironment.EnvironmentName;
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName() // [v16.0] ?⑤? ?꾩튂 媛곸씤
            .Enrich.WithProperty("InstanceId", instanceId) // [v16.0] ?몄뒪?댁뒪 ?앸퀎
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

    // [v4.0.0] ?ㅼ떆由ъ뒪???꾨졊: MassTransit 湲곕컲 怨좉??⑹꽦 硫붿떆吏??명봽??援ъ텞
    // Application ?꾨줈?앺듃??Consumer?ㅼ쓣 ?먮룞?쇰줈 ?ㅼ틪?섏뿬 ?깅줉?⑸땲??
    builder.Services.AddMessagingInfrastructure(builder.Configuration, typeof(MooldangBot.Application.Consumers.ChatReceivedConsumer).Assembly);

    // [?ㅼ떆由ъ뒪??寃??: ?고???DI ?뺥빀??理쒖쥌 ?뺤씤 媛??
    // 留뚯빟 ?명봽???쒕퉬???깅줉 怨쇱젙?먯꽌 ?꾨씫?섏뿀?ㅻ㈃ ?ш린??媛뺤젣濡?怨꾪넻???곌껐?⑸땲??
    if (!builder.Services.Any(x => x.ServiceType == typeof(MooldangBot.Application.Interfaces.IChzzkChatClient)))
    {
        Log.Warning("?좑툘 [DI Guard] IChzzkChatClient was missing from Infrastructure services. Forcing registration...");
        builder.Services.AddSingleton<MooldangBot.Application.Interfaces.IChzzkChatClient, MooldangBot.Infrastructure.ApiClients.GatewayChatClientProxy>();
    }

    // [v2.4.5] 移섏?吏??꾨Ц媛 ?깅줉? Infrastructure ?덉씠?댁쓽 AddInfrastructureServices?먯꽌 ?듯빀 愿由щ맗?덈떎.
    // [以묐났 ?쒓굅]: builder.Services.AddHttpClient<IChzzkApiClient, ChzzkApiClient>() 濡쒖쭅???명봽?쇰줈 ?대룞??
    // [v4.5.3] 梨꾪똿 ?대씪?댁뼵??愿由?二쇱껜??Infrastructure ?덉씠?대줈 寃⑸━??(GatewayChatClientProxy ?ъ슜)



    builder.Services.AddApplicationServices();
    builder.Services.AddWebApiWorkers(); // [v2.0] API ?꾩슜 ?뚯빱 ?깅줉 (Roulette, Zeroing ??
    
    // [v4.5.3] 10k TPS 怨좊?????묒쓣 ?꾪븳 MassTransit ?뚮퉬???뚯씠?꾨씪?몄씠 媛?숇릺?덉뒿?덈떎. (釉뚮┸吏 ?덉씠???쒓굅)
    // [v3.7] MassTransit Consumer媛 ????븷???泥댄븯誘濡??덇굅???섏쭛湲곕뒗 ?쒓굅?섏뿀?듬땲??
    
    builder.Services.AddPresentationServices();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IUserSession, UserSession>();

    // ?뱻 [吏?ν삎 愿묐????뚮굹]: MooldangBot 愿??紐⑤뱺 ?댁뀍釉붾━瑜?媛먯??섏뿬 MediatR ?몃뱾?щ? ?먮룞 ?깅줉?⑸땲?? (?ш컖吏? 諛⑹? 濡쒖쭅 ?ы븿)
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

    var redisUrl = builder.Configuration["REDIS_URL"]!; // [v22.0] ValidateMandatorySecrets???섑빐 蹂댁옣??
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

    // [?ㅼ떆由ъ뒪???쒕룞]: 寃利앸맂 ?곌껐 臾몄옄?댁쓣 ?ъ슜?섏뿬 媛?숈쓣 以鍮꾪빀?덈떎.
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

    builder.Services.AddStackExchangeRedisCache(options => {
        options.Configuration = redisUrl;
        options.InstanceName = "MooldangBot_";
    });


    builder.Services.AddHealthChecks().AddCheck<BotHealthCheck>("MooldangBot_Shards");
    builder.Services.ConfigureHttpJsonOptions(options => {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
    });

    // [?섎え?덉쓽 議곗쑉]: API ?띾룄 ?쒗븳 ?쒕퉬???깅줉
    builder.Services.AddMooldangRateLimiter();

    builder.Services.AddCors(options => {
        options.AddPolicy("IamfOverlayPolicy", policy => {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });

        // [?ㅼ떆由ъ뒪??以묎퀎]: Studio ?꾨줎?몄뿏???곕룞???꾪븳 ?꾩슜 CORS ?뺤콉
        options.AddPolicy("StudioCorsPolicy", policy => {
            policy.WithOrigins("http://localhost:3000", "https://www.mooldang.store") // 濡쒖뺄 諛??댁쁺 ?꾨찓???덉슜
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // ??以묒슂: ?몄쬆 荑좏궎(?몄뀡) 怨듭쑀瑜??꾪빐 ?꾩닔
        });
    });

    builder.Services.AddScoped<IAuthorizationHandler, OverlayTokenVersionHandler>();

    // ?뙥截?[Aegis Bridge]: ?꾨줉??Vite/Nginx) ?섍꼍?먯꽌???먮낯 IP 諛??꾨줈?좎퐳 ?꾨떖 ?ㅼ젙
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // 濡쒖뺄 媛쒕컻 ?섍꼍(localhost)?먯꽌???꾨줉?쒕뒗 ?좊ː?섎룄濡??ㅼ젙
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // ?썳截?[?댁??ㅼ쓽 諛⑺뙣]: API 踰꾩쟾 愿由??깅줉
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true; // 踰꾩쟾 紐낆떆 ?????덇굅???붿껌? 1.0?쇰줈 媛꾩＜
        options.ReportApiVersions = true; // ?묐떟 ?ㅻ뜑??吏?먰븯??踰꾩쟾 紐낆떆
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version") // ?ㅻ뜑 諛⑹떇???덈퉬濡?吏??
        );
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // ?㏈ [?댁??ㅼ쓽 ?뺥솕]: FluentValidation ?먮룞 寃利??깅줉
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssembly(typeof(MooldangBot.Application.Interfaces.IAppDbContext).Assembly);

    builder.Services.AddControllers()
        .AddApplicationPart(typeof(MooldangBot.Presentation.DependencyInjection).Assembly)
        .AddJsonOptions(options => {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ChzzkJsonContext.Default);
        });

    // ?뱰 [?ㅼ떆由ъ뒪???쒓퀬]: Swagger/OpenAPI ?ㅼ쟾 臾몄꽌???ㅼ젙
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "MooldangBot API", 
            Version = "v1.1",
            Description = "[?섎え?덉쓽 ?꾪솕吏]: 臾쇰뙐遊?諛깆뿏???듯빀 API 紐낆꽭?쒖엯?덈떎."
        });

        // JWT ?몄쬆 UI 異붽?
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        // OpenAPI 2.x (OAS 3.x) ?뚮떎 湲곕컲 蹂댁븞 ?붽뎄?ы빆 洹쒓꺽 ?곸슜
        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", null, null)] = new List<string>()
        });
    });

    // [v4.0] ?ㅼ떆由ъ뒪???댁뇿: JWT ?쒗겕由???寃利?(?꾩슜 寃利앹? ValidateMandatorySecrets?먯꽌 ?섑뻾??
    var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
    
    if (Encoding.UTF8.GetBytes(jwtSecret).Length < 32)
    {
        throw new InvalidOperationException("?뵦 [Security Error]: JWT ?쒗겕由??ㅺ? ?덈Т 吏㏃뒿?덈떎. 理쒖냼 32諛붿씠??256鍮꾪듃) ?댁긽???ㅻ? ?ъ슜?섏떗?쒖삤.");
    }
    var issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

    builder.Services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options => { 
        options.LoginPath = "/api/auth/chzzk-login"; 
        options.Cookie.Name = builder.Configuration["AUTH_COOKIE_NAME"] ?? ".MooldangBot.Session";
        
        // [Aegis 濡쒖뺄 ?⑥뒪]: 濡쒖뺄/HTTP ?섍꼍?먯꽌???몄뀡???좎??섎룄濡?蹂댁븞 ?뺤콉 議곗젙
        var isDev = builder.Environment.IsDevelopment();
        options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax; // [v22.2] ?숈씪 ?꾨찓???섍꼍?먯꽌???몄뀡 ?좎???媛뺥솕 (Lax 沅뚯옣)
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true; // [v22.1] ?꾩닔 荑좏궎濡?吏?뺥븯???쒖뒪??媛?⑹꽦 蹂댁옣
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
                // SignalR? 荑쇰━ ?ㅽ듃留?"access_token")?쇰줈 ?좏겙??蹂대궪 ???덉쑝誘濡??대? ?싳븘梨뺣땲??
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
        // [?ㅼ떆由ъ뒪???듯빀 ?뺤콉]: 荑좏궎? JWT ?몄쬆??紐⑤몢 ?덉슜?섎뒗 湲곕낯 ?멸? ?뺤콉
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme)
            .Build();

        options.AddPolicy("ChannelManager", policy => {
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new ChannelManagerRequirement());
        });

        // [?ㅼ떆由ъ뒪??怨듬챸]: ?ㅻ쾭?덉씠 ?꾩슜 (JWT 沅뚯옣 + 踰꾩쟾 寃利? ?뺤콉
        options.AddPolicy("OverlayAuth", policy => {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.Requirements.Add(new OverlayTokenVersionRequirement()); // ?뵍 踰꾩쟾 ?ㅼ떆媛?寃利?
        });
    });

    var app = builder.Build();

    // 1. [?ㅼ떆由ъ뒪???ы뙋]: 媛??癒쇱? ?덉쇅瑜??싳븘梨꾧린 ?꾪빐 理쒖긽?⑥뿉 諛곗튂
    app.UseMiddleware<ExceptionMiddleware>();

    // 2. ?ъ뒪泥댄겕 濡쒓렇 ?ㅽ뙵 諛⑹? (?뺤긽????Verbose, ?먮윭????Error)
    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (ctx, _, ex) => 
            ex != null || ctx.Response.StatusCode > 499 ? Serilog.Events.LogEventLevel.Error :
            ctx.Request.Path.StartsWithSegments("/health") ? Serilog.Events.LogEventLevel.Verbose : 
            Serilog.Events.LogEventLevel.Information;
    });

    app.UseForwardedHeaders();
    app.UseStaticFiles(); // [?섎え?덉쓽 ?붾옉]: ?낅줈?쒕맂 ?대?吏 ???뺤쟻 ?뚯씪 ?쒕튃 ?쒖꽦??
    app.UseRouting();

    // ?썳截?[?ㅼ떆由ъ뒪???댁뇿]: ?띾룄 ?쒗븳? ?쇱슦??吏곹썑, CORS? ?④퍡 諛곗튂
    app.UseRateLimiter();
    // [?ㅼ떆由ъ뒪??以묎퀎]: 二?議고???Studio)???꾪븳 ?먭꺽 利앸챸 ?덉슜 ?뺤콉 ?곗꽑 ?곸슜
    app.UseCors("StudioCorsPolicy"); 
    app.UseCors("IamfOverlayPolicy"); // [v6.2] ?덇굅???ㅻ쾭?덉씠 ?명솚???좎?

    app.UseAuthentication();
    app.UseAuthorization();

    // 3. ?렧 [?섎え?덉쓽 湲곕줉]: 諛섎뱶???몄쬆(Authorization) 吏곹썑??諛곗튂?댁빞 User ?뺣낫媛 議댁옱??
    app.UseMiddleware<LogEnrichmentMiddleware>();

    // app.MapGet("/", () => Results.Redirect("/swagger")); // [v2.4.1] Prometheus 硫뷀듃由?誘몃뱾?⑥뼱 諛??붾뱶?ъ씤???몄텧
    app.UseHttpMetrics();

    // ---------------------------------------------------------
    // [理쒗썑??蹂대（]: ?좏뵆由ъ??댁뀡 ?쒖옉
    // ---------------------------------------------------------
    app.MapControllers();
    app.MapMetrics(); // /metrics ?붾뱶?ъ씤??留ㅽ븨
    app.MapHealthChecks("/health"); // [?ㅼ떆由ъ뒪??諛뺣룞]: 而⑦뀒?대꼫 ?곹깭 媛먯떆 ?꾩슜 ?붾뱶?ъ씤??
    app.MapHub<OverlayHub>("/overlayHub");

    // [v4.9] Swagger UI ?쒖꽦??(蹂댁븞???꾪빐 媛쒕컻 ?섍꼍 ?먮뒗 紐낆떆???ㅼ젙 ?쒖뿉留??몄텧)
    if (app.Environment.IsDevelopment() || app.Configuration["EnableSwagger"] == "true")
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MooldangBot API v1.1");
        });
    }

    // [?ㅼ떆由ъ뒪???쒕룞]: ?쒖뒪??珥덇린??(DB ?쒕뵫 諛??쒕퉬??湲곕룞)
    using (var scope = app.Services.CreateScope())
    {
        await scope.ServiceProvider.GetRequiredService<IDbInitializer>().InitializeAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "?뵦 [?ш컖???ㅻ쪟 諛쒖깮]: ?좏뵆由ъ??댁뀡 湲곕룞 以?蹂듦뎄 遺덇??ν븳 ?덉쇅媛 諛쒖깮?덉뒿?덈떎.");
    throw;
}
finally
{
    Log.Information("?몝 [?ㅼ떆由ъ뒪???됱삩]: ?좏뵆由ъ??댁뀡???덉쟾?섍쾶 醫낅즺?⑸땲??");
    Log.CloseAndFlush();
}
