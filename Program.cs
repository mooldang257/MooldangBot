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

// 💡 서버 이동 시 여기 주소만 바꾸면 됩니다! (Cloudflare 8443 대응)
string baseDomain = "http://localhost:3000"; 
//string baseDomain = "https://your-domain.com:8443";

// ==========================================
// 3. 🌐 화면 라우팅 (View)
// ==========================================

app.MapGet("/", async (HttpContext context, AppDbContext db) => {
    if (context.User.Identity?.IsAuthenticated != true) return Results.Redirect("/login");

    var naverId = context.User.FindFirstValue("StreamerId");
    var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId);

    if (streamer == null || string.IsNullOrEmpty(streamer.ChzzkUid)) return Results.Redirect("/setup");
    if (string.IsNullOrEmpty(streamer.ChzzkAccessToken)) return Results.Redirect("/api/auth/chzzk-login");

    return Results.Redirect($"/dashboard/{streamer.ChzzkUid}");
});

app.MapGet("/login", async context => {
    await context.ChallengeAsync(NaverAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
});

// 설정/관리 화면들
app.MapGet("/setup", async (HttpContext context) => {
    // 💡 Results.File을 사용하여 명시적으로 IResult를 반환합니다.
    return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/setup.html"), "text/html; charset=utf-8");
}).RequireAuthorization();

app.MapGet("/dashboard/{chzzkUid}", async (string chzzkUid, HttpContext context, AppDbContext db) => {
    var naverId = context.User.FindFirstValue("StreamerId");
    var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId);

    if (profile == null || profile.ChzzkUid != chzzkUid)
        return Results.Redirect("/");

    // 💡 마지막에 반드시 return이 있어야 합니다.
    return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/dashboard.html"), "text/html; charset=utf-8");
}).RequireAuthorization();

app.MapGet("/commands-manager/{chzzkUid}", async (string chzzkUid, HttpContext context, AppDbContext db) => {
    var naverId = context.User.FindFirstValue("StreamerId");
    var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId);

    if (profile == null || profile.ChzzkUid != chzzkUid)
        return Results.Redirect("/");

    // 💡 여기도 마찬가지로 return을 추가합니다.
    return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commands.html"), "text/html; charset=utf-8");
}).RequireAuthorization();

app.MapGet("/overlay/{chzzkUid}", async context => {
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync("wwwroot/overlay.html");
});

// ==========================================
// 4. 🔐 치지직 공식 인증 (OAuth)
// ==========================================

app.MapGet("/api/auth/chzzk-login", async (AppDbContext db, HttpContext context) => {
    var clientIdConf = await db.SystemSettings.FindAsync("ChzzkClientId");
    string clientId = clientIdConf?.KeyValue ?? "";
    string redirectUri = $"{baseDomain}/Auth/callback";
    string state = Guid.NewGuid().ToString();

    string authUrl = $"https://chzzk.naver.com/account-interlock?clientId={clientId}&redirectUri={redirectUri}&state={state}";
    context.Response.Redirect(authUrl);
});

app.MapGet("/Auth/callback", async (HttpContext context, AppDbContext db) => {
    string? code = context.Request.Query["code"];
    string? state = context.Request.Query["state"];
    if (string.IsNullOrEmpty(code)) return Results.Text("인증 코드가 없습니다.");

    try
    {
        var clientIdConf = await db.SystemSettings.FindAsync("ChzzkClientId");
        var clientSecretConf = await db.SystemSettings.FindAsync("ChzzkClientSecret");

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var tokenRequest = new
        {
            grantType = "authorization_code",
            clientId = clientIdConf?.KeyValue,
            clientSecret = clientSecretConf?.KeyValue,
            code = code,
            state = state
        };

        var response = await httpClient.PostAsJsonAsync("https://openapi.chzzk.naver.com/auth/v1/token", tokenRequest);
        if (!response.IsSuccessStatusCode) return Results.Text($"토큰 발급 실패: {await response.Content.ReadAsStringAsync()}");

        var tokenContent = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("content");
        string accessToken = tokenContent.GetProperty("accessToken").GetString()!;
        string refreshToken = tokenContent.GetProperty("refreshToken").GetString()!;
        int expiresIn = tokenContent.GetProperty("expiresIn").GetInt32();

        // 치지직 UID 조회
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var profileRes = await httpClient.GetFromJsonAsync<JsonElement>("https://openapi.chzzk.naver.com/open/v1/users/me");
        string chzzkUid = profileRes.GetProperty("content").GetProperty("channelId").GetString()!;

        // DB 저장 (기존 정보 있으면 업데이트, 없으면 신규)
        var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
        if (streamer == null)
        {
            streamer = new StreamerProfile { ChzzkUid = chzzkUid, NaverId = context.User.FindFirstValue("StreamerId")! };
            db.StreamerProfiles.Add(streamer);
        }
        streamer.ChzzkAccessToken = accessToken;
        streamer.ChzzkRefreshToken = refreshToken;
        streamer.TokenExpiresAt = DateTime.Now.AddSeconds(expiresIn);

        await db.SaveChangesAsync();
        return Results.Redirect($"/dashboard/{chzzkUid}");
    }
    catch (Exception ex) { return Results.Text($"에러 발생: {ex.Message}"); }
}).AllowAnonymous();

// ==========================================
// 5. 🚀 데이터 관리 API (Song, Command, Settings)
// ==========================================

// 대시보드 데이터 조회
app.MapGet("/api/dashboard/data/{chzzkUid}", async (string chzzkUid, AppDbContext db) => {
    var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
    var songs = await db.SongQueues.Where(s => s.ChzzkUid == chzzkUid).OrderBy(s => s.SortOrder).ThenBy(s => s.CreatedAt).ToListAsync();
    return Results.Ok(new { memo = profile?.NoticeMemo ?? "", omakaseCount = profile?.OmakaseCount ?? 0, songs });
});

// 신청곡 관리 (Add, Edit, Status, Delete)
app.MapPost("/api/song/add", async (SongQueue newSong, AppDbContext db) => {
    newSong.CreatedAt = DateTime.Now; db.SongQueues.Add(newSong); await db.SaveChangesAsync(); return Results.Ok(newSong);
});

app.MapPut("/api/song/{id}/status", async (int id, string status, AppDbContext db) => {
    var song = await db.SongQueues.FindAsync(id);
    if (song != null)
    {
        if (status == "Playing")
        {
            var current = await db.SongQueues.FirstOrDefaultAsync(s => s.ChzzkUid == song.ChzzkUid && s.Status == "Playing");
            if (current != null) current.Status = "Completed";
        }
        song.Status = status; await db.SaveChangesAsync();
    }
    return Results.Ok();
});

app.MapPost("/api/song/delete", async (List<int> ids, AppDbContext db) => {
    var songs = await db.SongQueues.Where(s => ids.Contains(s.Id)).ToListAsync();
    db.SongQueues.RemoveRange(songs); await db.SaveChangesAsync(); return Results.Ok();
});

// 동적 명령어 관리 (List, Save, Delete)
app.MapGet("/api/commands/list/{chzzkUid}", async (string chzzkUid, AppDbContext db) =>
    Results.Ok(await db.StreamerCommands.Where(c => c.ChzzkUid == chzzkUid).ToListAsync()));

app.MapPost("/api/commands/save", async (StreamerCommand cmd, AppDbContext db) => {
    var existing = await db.StreamerCommands.FirstOrDefaultAsync(c => c.ChzzkUid == cmd.ChzzkUid && c.CommandKeyword == cmd.CommandKeyword);
    if (existing == null) db.StreamerCommands.Add(cmd);
    else { existing.ActionType = cmd.ActionType; existing.Content = cmd.Content; existing.RequiredRole = cmd.RequiredRole; }
    await db.SaveChangesAsync(); return Results.Ok();
});

app.MapDelete("/api/commands/delete/{id}", async (int id, AppDbContext db) => {
    var cmd = await db.StreamerCommands.FindAsync(id);
    if (cmd != null) { db.StreamerCommands.Remove(cmd); await db.SaveChangesAsync(); }
    return Results.Ok();
});

// 스트리머 설정 업데이트
app.MapPost("/api/settings/update", async (string chzzkUid, SettingsUpdateRequest req, AppDbContext db) => {
    var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == chzzkUid);
    if (profile != null)
    {
        profile.SongCommand = req.SongCommand; profile.SongCheesePrice = req.SongCheesePrice;
        profile.OmakaseCommand = req.OmakaseCommand; profile.OmakaseCheesePrice = req.OmakaseCheesePrice;
        profile.DesignSettingsJson = req.DesignSettingsJson;
        await db.SaveChangesAsync();
    }
    return Results.Ok();
});

// 초기 셋업 API
app.MapPost("/api/setup", async (SetupRequest request, HttpContext context, AppDbContext db) => {
    var naverId = context.User.FindFirstValue("StreamerId");
    if (string.IsNullOrEmpty(naverId)) return Results.Unauthorized();
    var profile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId) ?? new StreamerProfile { NaverId = naverId };
    profile.ChzzkUid = request.ChzzkUid;
    if (profile.Id == 0) db.StreamerProfiles.Add(profile);
    await db.SaveChangesAsync(); return Results.Ok();
}).RequireAuthorization();

app.MapHub<OverlayHub>("/overlayHub");

// 8443 포트 사용을 위해 ASPNETCORE_URLS 환경변수를 쓰거나 아래를 수정하세요.
app.Run();

// ==========================================
// 6. 데이터 클래스
// ==========================================
public class SetupRequest { public string ChzzkUid { get; set; } = ""; }
public class SettingsUpdateRequest
{
    public string SongCommand { get; set; } = "!신청";
    public int SongCheesePrice { get; set; } = 0;
    public string OmakaseCommand { get; set; } = "!오마카세";
    public int OmakaseCheesePrice { get; set; } = 1000;
    public string DesignSettingsJson { get; set; } = "{}";
}