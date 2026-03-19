using AspNet.Security.OAuth.Naver;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Hubs;
using MooldangAPI.Models;
using MooldangAPI.Services;
using System.Security.Claims;
using System.Text.Json;
using MediatR;
using MooldangAPI.Features.SongQueue;
using MooldangAPI.Features.Roulette;
using MooldangAPI.Strategies;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. 핵심 인프라 및 DB 설정 수정테스트
// ==========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// -- Event-Driven Architecture 의존성 주입 --
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddSingleton<SongQueueState>();
builder.Services.AddSingleton<RouletteState>();
builder.Services.AddTransient<IOverlayRenderStrategy, DefaultChatRenderStrategy>();
builder.Services.AddHostedService<ChzzkBackgroundService>();
// ------------------------------------------

builder.Services.AddSignalR();
builder.Services.AddControllers();

// Removed BotManager. ChzzkBackgroundService handles this via EDA.

// ==========================================
// 2. 네이버 로그인(문지기) 설정
// ==========================================
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = NaverAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options => { options.LoginPath = "/login"; })
.AddNaver(options => {
    // appsettings.json 또는 환경 변수에서 값을 가져옵니다.
    options.ClientId = builder.Configuration["NaverOAuth:ClientId"] ?? throw new InvalidOperationException("Naver ClientId is missing.");
    options.ClientSecret = builder.Configuration["NaverOAuth:ClientSecret"] ?? throw new InvalidOperationException("Naver ClientSecret is missing.");

    options.Events.OnCreatingTicket = context => {
        if (context.User.TryGetProperty("response", out var responseElement) &&
            responseElement.TryGetProperty("id", out var idElement))
        {
            var naverId = idElement.GetString();
            var identity = (ClaimsIdentity)context.Principal!.Identity!;
            if (!string.IsNullOrEmpty(naverId)) identity.AddClaim(new Claim("StreamerId", naverId));
        }
        return Task.CompletedTask;
    };

    // 💡 프록시 환경에서 Redirect URI가 http로 생성되는 문제를 방지하기 위해 강제로 https로 변환합니다.
    options.Events.OnRedirectToAuthorizationEndpoint = context => {
        var redirectUri = context.RedirectUri;
        if (redirectUri.Contains("redirect_uri=http%3A%2F%2F"))
        {
            redirectUri = redirectUri.Replace("redirect_uri=http%3A%2F%2F", "redirect_uri=https%3A%2F%2F");
        }
        else if (redirectUri.Contains("redirect_uri=http://"))
        {
            redirectUri = redirectUri.Replace("redirect_uri=http://", "redirect_uri=https://");
        }

        // 🔍 [로그 추가] 네이버로 보내는 최종 주소를 서버 콘솔에 출력하여 디버깅합니다.
        Console.WriteLine($"[NaverAuth] Redirecting to Naver: {redirectUri}");
        
        context.Response.Redirect(redirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();
var app = builder.Build();

// ==========================================
// ⭐ [추가] 프록시(Cloudflare, Nginx) 대응 설정
// ==========================================
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // 💡 클라우드플레어 터널 등 프록시 환경에서 프로토콜(HTTPS) 정보를 정확히 읽어오도록 신뢰 설정을 추가합니다.
    KnownNetworks = { },
    KnownProxies = { },
    ForwardLimit = null // 프록시 제한을 풀어서 모든 홉을 신뢰하게 함
});

// 미들웨어 설정
app.UseStaticFiles();
app.UseAuthentication();
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
    db.Database.Migrate();

    // 💡 [DB 초기값 세팅] 리눅스 도커 환경에서 DB가 초기화되었을 때 appsettings의 값을 DB에 자동으로 채워줍니다.
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    void EnsureSetting(string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (!db.SystemSettings.Any(s => s.KeyName == key))
        {
            db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
        }
    }

    EnsureSetting("ChzzkClientId", config["ChzzkApi:ClientId"]);
    EnsureSetting("ChzzkClientSecret", config["ChzzkApi:ClientSecret"]);
    db.SaveChanges();
}

app.Run();

