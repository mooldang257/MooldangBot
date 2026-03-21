using Microsoft.AspNetCore.Mvc;
using MooldangAPI.Data;
using MooldangAPI.Models;
using System.Text.Json;
using System.Text;
using MooldangAPI.Services;

namespace MooldangAPI.Controllers
{
    public class SyncRequest
    {
        public string? Keyword { get; set; }
    }

    [ApiController]
    [Route("api/admin/bot")]
    public class AdminBotController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public AdminBotController(AppDbContext db, IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _db = db;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
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

        // 2. 카테고리 동기화 상태 조회
        [HttpGet("sync-status")]
        public IActionResult GetSyncStatus()
        {
            return Ok(new
            {
                isRunning = ChzzkCategorySyncService.IsRunning,
                lastRunTime = ChzzkCategorySyncService.LastRunTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                lastResult = ChzzkCategorySyncService.LastResult,
                addedCount = ChzzkCategorySyncService.LastAddedCount,
                updatedCount = ChzzkCategorySyncService.LastUpdatedCount
            });
        }

        // 3. 카테고리 동기화 수동 시작
        [HttpPost("sync-categories")]
        public async Task<IActionResult> StartSync([FromBody] SyncRequest? req = null)
        {
            if (ChzzkCategorySyncService.IsRunning)
            {
                return BadRequest(new { message = "이미 동기화가 진행 중입니다." });
            }

            var specificKeyword = req?.Keyword;

            if (!string.IsNullOrEmpty(specificKeyword))
            {
                // 단일 키워드 검색: 대기 후 즉시 결과 반환
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ChzzkCategorySyncService>();
                var results = await syncService.SearchAndSaveCategoryAsync(specificKeyword);

                return Ok(new 
                { 
                    message = $"'{specificKeyword}' 키워드로 {results.Count}개의 카테고리를 동기화했습니다.",
                    results = results
                });
            }

            // 백그라운드에서 실행되도록 Fire and Forget 방식으로 호출 (전체 동기화)
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ChzzkCategorySyncService>();
                await syncService.SyncCategoriesAsync(null);
            });

            return Ok(new { message = "전체 카테고리 동기화를 시작했습니다." });
        }
    }
}