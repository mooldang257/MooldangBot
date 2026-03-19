using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using MooldangAPI.Models;
using System.Text.Json;
using System.Text;

namespace MooldangAPI.Controllers
{
    [ApiController]
    [Route("api/admin/bot")]
    public class AdminBotController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public AdminBotController(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        // 1. 봇 연동 시작 (브라우저에서 이 주소로 접속)
        [HttpGet("login")]
        public IActionResult BotLogin()
        {
            string clientId = ApiClients.SecretGuardian.GetClientId();
            string baseDomain = _configuration["BaseDomain"] ?? "https://www.mooldang.store";
            string redirectUri = $"{baseDomain}/Auth/callback"; 
            string state = "bot_setup_" + Guid.NewGuid().ToString();

            string chzzkAuthUrl = $"https://chzzk.naver.com/account-interlock?clientId={clientId}&redirectUri={redirectUri}&state={state}";
            return Redirect(chzzkAuthUrl);
        }

        // 💡 봇 콜백(/Auth/callback)은 이제 AuthController에서 통합 처리합니다. 
        // 기존의 BotCallback 메소드는 삭제합니다.
    }

        private void UpdateOrAddSetting(string key, string value)
        {
            var setting = _db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
            if (setting == null) _db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
            else setting.KeyValue = value;
        }
    }
}