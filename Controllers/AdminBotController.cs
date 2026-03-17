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

        public AdminBotController(AppDbContext db)
        {
            _db = db;
        }

        // 1. 봇 연동 시작 (브라우저에서 이 주소로 접속)
        [HttpGet("login")]
        public IActionResult BotLogin()
        {
            // SecretGuardian이나 환경변수에서 ClientId 가져오기
            string clientId = ApiClients.SecretGuardian.GetClientId();
            string redirectUri = "http://localhost:5000/api/admin/bot/callback"; // API 서버 주소에 맞게 변경
            string state = "bot_setup";

            string chzzkAuthUrl = $"https://chzzk.naver.com/meta/oauth/authorize?clientId={clientId}&redirectUri={redirectUri}&state={state}";
            return Redirect(chzzkAuthUrl);
        }

        // 2. 치지직 인증 콜백 (토큰 수령 후 SystemSettings에 저장)
        [HttpGet("callback")]
        public async Task<IActionResult> BotCallback([FromQuery] string code, [FromQuery] string state)
        {
            string clientId = ApiClients.SecretGuardian.GetClientId();
            string clientSecret = ApiClients.SecretGuardian.GetClientSecret();

            using var httpClient = new HttpClient();
            var tokenReq = new { grantType = "authorization_code", clientId, clientSecret, code, state };

            var res = await httpClient.PostAsync("https://openapi.chzzk.naver.com/auth/v1/token",
                new StringContent(JsonSerializer.Serialize(tokenReq), Encoding.UTF8, "application/json"));

            if (!res.IsSuccessStatusCode)
                return BadRequest("토큰 연성 실패: " + await res.Content.ReadAsStringAsync());

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement.GetProperty("content");

            string accessToken = content.GetProperty("accessToken").GetString() ?? "";
            string refreshToken = content.GetProperty("refreshToken").GetString() ?? "";
            DateTime expireDate = DateTime.Now.AddSeconds(content.GetProperty("expiresIn").GetInt32());

            // DB 저장 로직
            UpdateOrAddSetting("BotAccessToken", accessToken);
            UpdateOrAddSetting("BotRefreshToken", refreshToken);
            UpdateOrAddSetting("BotTokenExpiresAt", expireDate.ToString("O"));

            await _db.SaveChangesAsync();

            return Ok("🎉 시스템 기본 봇(물댕봇) 계정 연동이 성공적으로 완료되었습니다! 이제 창을 닫아도 됩니다.");
        }

        private void UpdateOrAddSetting(string key, string value)
        {
            var setting = _db.SystemSettings.FirstOrDefault(s => s.KeyName == key);
            if (setting == null) _db.SystemSettings.Add(new SystemSetting { KeyName = key, KeyValue = value });
            else setting.KeyValue = value;
        }
    }
}