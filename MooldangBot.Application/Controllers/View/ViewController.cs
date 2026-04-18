using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace MooldangBot.Application.Controllers.View
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)] // [v10.1] Swagger 등 API 문서에서 제외
    // [v10.1] Primary Constructor 적용
    public class ViewController(IWebHostEnvironment env) : ControllerBase
    {
        [HttpGet("/bot")]
        [AllowAnonymous]
        public IActionResult Index()
        {
            var htmlPath = Path.Combine(env.WebRootPath, "main.html");
            if (!System.IO.File.Exists(htmlPath)) return NotFound();
            return PhysicalFile(htmlPath, "text/html; charset=utf-8");
        }

        [HttpGet("/songlist_settings/{chzzkUid}")]
        [Authorize(Policy = "ChannelManager")]
        public IActionResult SettingsPage(string chzzkUid)
        {
            var htmlPath = Path.Combine(env.WebRootPath, "songlist_settings.html");
            if (!System.IO.File.Exists(htmlPath)) return NotFound();
            return PhysicalFile(htmlPath, "text/html; charset=utf-8");
        }

        [HttpGet("/login")]
        public IActionResult Login()
        {
            return Redirect("/api/auth/chzzk-login");
        }

        [HttpGet("/songlist/{chzzkUid}")]
        [Authorize(Policy = "ChannelManager")]
        public IActionResult DashboardPage(string chzzkUid)
        {
            var htmlPath = Path.Combine(env.WebRootPath, "songlist.html");
            if (!System.IO.File.Exists(htmlPath)) return NotFound();
            return PhysicalFile(htmlPath, "text/html; charset=utf-8");
        }

        [HttpGet("/commands-manager/{chzzkUid}")]
        [Authorize(Policy = "ChannelManager")]
        public IActionResult CommandsManagerPage(string chzzkUid)
        {
            var htmlPath = Path.Combine(env.WebRootPath, "commands.html");
            if (!System.IO.File.Exists(htmlPath)) return NotFound();
            return PhysicalFile(htmlPath, "text/html; charset=utf-8");
        }

        [HttpGet("/overlay_manager/{chzzkUid}")]
        [Authorize(Policy = "ChannelManager")]
        public IActionResult OverlayManagerPage(string chzzkUid)
        {
            var htmlPath = Path.Combine(env.WebRootPath, "overlay_manager.html");
            if (!System.IO.File.Exists(htmlPath)) return NotFound();
            return PhysicalFile(htmlPath, "text/html; charset=utf-8");
        }


        [HttpGet("/songlist_overlay/{chzzkUid}")]
        public IActionResult SonglistOverlayPage(string chzzkUid)
        {
            var htmlPath = Path.Combine(env.WebRootPath, "songlist_overlay.html");
            if (!System.IO.File.Exists(htmlPath)) return NotFound();
            return PhysicalFile(htmlPath, "text/html; charset=utf-8");
        }

        [HttpGet("/roulette_overlay/{chzzkUid}")]
        public IActionResult RouletteOverlayPage(string chzzkUid)
        {
            var htmlPath = Path.Combine(env.WebRootPath, "roulette_overlay.html");
            if (!System.IO.File.Exists(htmlPath)) return NotFound();
            return PhysicalFile(htmlPath, "text/html; charset=utf-8");
        }

        [HttpGet("/avatar_overlay/{chzzkUid}")]
        public IActionResult AvatarOverlayPage(string chzzkUid)
        {
            var htmlPath = Path.Combine(env.WebRootPath, "avatar_overlay.html");
            if (!System.IO.File.Exists(htmlPath)) return NotFound();
            return PhysicalFile(htmlPath, "text/html; charset=utf-8");
        }

        [HttpGet("/overlay/{chzzkUid}")]
        public IActionResult OverlayPage(string chzzkUid)
        {
            var htmlPath = Path.Combine(env.WebRootPath, "overlay.html");
            if (!System.IO.File.Exists(htmlPath)) return NotFound();
            return PhysicalFile(htmlPath, "text/html; charset=utf-8");
        }
    }
}
