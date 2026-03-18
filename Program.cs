using AspNet.Security.OAuth.Naver;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Hubs;
using MooldangAPI.Models;
using MooldangAPI.Services;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. 핵심 인프라 및 DB 설정
// ==========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddSignalR();
builder.Services.AddControllers();

// 백그라운드 봇 서비스 등록 (중복 방지)
builder.Services.AddSingleton<BotManager>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<BotManager>());

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
});

builder.Services.AddAuthorization();
var app = builder.Build();

// 미들웨어 설정
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();


// ==========================================
// 3. 🌐 화면 라우팅 (View)
// ==========================================



// ==========================================
// 4. 🔐 치지직 공식 인증 (OAuth)
// ==========================================

app.MapControllers();




app.MapHub<OverlayHub>("/overlayHub");

// 8443 포트 사용을 위해 ASPNETCORE_URLS 환경변수를 쓰거나 아래를 수정하세요.
app.Run();

