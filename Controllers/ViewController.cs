using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using AspNet.Security.OAuth.Naver;

namespace MooldangAPI.Controllers
{
    [ApiController]
    public class ViewController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ViewController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("/")]
        public async Task<IResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true) return Results.Redirect("/login");

            var naverId = User.FindFirstValue("StreamerId");
            var streamer = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId);

            if (streamer == null || string.IsNullOrEmpty(streamer.ChzzkUid) || string.IsNullOrEmpty(streamer.ChzzkAccessToken))
            {
                return Results.Redirect("/api/auth/chzzk-login");
            }

            return Results.Redirect($"/dashboard/{streamer.ChzzkUid}");
        }

        [HttpGet("/settings/{chzzkUid}")]
        [Authorize]
        public async Task<IResult> SettingsPage(string chzzkUid)
        {
            var naverId = User.FindFirstValue("StreamerId");
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId);

            if (profile == null || profile.ChzzkUid != chzzkUid)
                return Results.Redirect("/");

            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/settings.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/login")]
        public IResult Login()
        {
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = "/" },
                new[] { NaverAuthenticationDefaults.AuthenticationScheme }
            );
        }

        [HttpGet("/dashboard/{chzzkUid}")]
        [Authorize]
        public async Task<IResult> DashboardPage(string chzzkUid)
        {
            var naverId = User.FindFirstValue("StreamerId");
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId);

            if (profile == null || profile.ChzzkUid != chzzkUid)
                return Results.Redirect("/");

            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/dashboard.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/commands-manager/{chzzkUid}")]
        [Authorize]
        public async Task<IResult> CommandsManagerPage(string chzzkUid)
        {
            var naverId = User.FindFirstValue("StreamerId");
            var profile = await _db.StreamerProfiles.FirstOrDefaultAsync(p => p.NaverId == naverId);

            if (profile == null || profile.ChzzkUid != chzzkUid)
                return Results.Redirect("/");

            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commands.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/overlay/{chzzkUid}")]
        public IResult OverlayPage(string chzzkUid)
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/overlay.html"), "text/html; charset=utf-8");
        }
    }
}
