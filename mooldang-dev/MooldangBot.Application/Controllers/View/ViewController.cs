using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace MooldangBot.Application.Controllers.View
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ViewController() : ControllerBase
    {
        [HttpGet("/bot")]
        [AllowAnonymous]
        public IActionResult Index() => Redirect("/");

        [HttpGet("/login")]
        public IActionResult Login() => Redirect("/api/auth/chzzk-login");
    }
}
