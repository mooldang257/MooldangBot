using Microsoft.AspNetCore.Mvc;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace MooldangBot.Presentation.Features.View
{
    [ApiController]
    public class ViewController : ControllerBase
    {
        private readonly IAppDbContext _db;

        public ViewController(IAppDbContext db)
        {
            _db = db;
        }

        [HttpGet("/bot")]
        [AllowAnonymous]
        public IResult Index()
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/main.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/songlist_settings/{chzzkUid}")]
        [Authorize(Policy = "ChannelManager")]
        public IResult SettingsPage(string chzzkUid)
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/songlist_settings.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/login")]
        public IResult Login()
        {
            return Results.Redirect("/api/auth/chzzk-login");
        }

        [HttpGet("/songlist/{chzzkUid}")]
        [Authorize(Policy = "ChannelManager")]
        public IResult DashboardPage(string chzzkUid)
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/songlist.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/commands-manager/{chzzkUid}")]
        [Authorize(Policy = "ChannelManager")]
        public IResult CommandsManagerPage(string chzzkUid)
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commands.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/overlay_manager/{chzzkUid}")]
        [Authorize(Policy = "ChannelManager")]
        public IResult OverlayManagerPage(string chzzkUid)
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/overlay_manager.html"), "text/html; charset=utf-8");
        }


        [HttpGet("/songlist_overlay/{chzzkUid}")]
        public IResult SonglistOverlayPage(string chzzkUid)
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/songlist_overlay.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/roulette_overlay/{chzzkUid}")]
        public IResult RouletteOverlayPage(string chzzkUid)
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/roulette_overlay.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/avatar_overlay/{chzzkUid}")]
        public IResult AvatarOverlayPage(string chzzkUid)
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/avatar_overlay.html"), "text/html; charset=utf-8");
        }

        [HttpGet("/overlay/{chzzkUid}")]
        public IResult OverlayPage(string chzzkUid)
        {
            return Results.File(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/overlay.html"), "text/html; charset=utf-8");
        }
    }
}
