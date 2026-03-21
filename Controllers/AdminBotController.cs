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

    public class AliasRequest
    {
        public string Alias { get; set; } = string.Empty;
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

        // ==========================================
        // 카테고리 및 약어 관리 API 파트
        // ==========================================

        [HttpGet("categories")]
        public async Task<IActionResult> SearchCategories([FromQuery] string? search)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = db.ChzzkCategories.Include(c => c.Aliases).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(c => c.CategoryValue.Contains(s) || c.CategoryId.Contains(s) || c.Aliases.Any(a => a.Alias.Contains(s)));
            }

            // 검색어가 없을 때는 최신 업데이트 순서대로 최대 100개만
            var results = await query.OrderByDescending(c => c.UpdatedAt).Take(100).ToListAsync();

            return Ok(results);
        }

        [HttpPost("categories/{categoryId}/aliases")]
        public async Task<IActionResult> AddCategoryAlias(string categoryId, [FromBody] AliasRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Alias))
                return BadRequest("약어(Alias)를 올바르게 입력해주세요.");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var category = await db.ChzzkCategories.FindAsync(categoryId);
            if (category == null)
                return NotFound("존재하지 않는 카테고리입니다.");

            var aliasName = request.Alias.Trim();

            // 중복 검사
            if (await db.ChzzkCategoryAliases.AnyAsync(a => a.CategoryId == categoryId && a.Alias == aliasName))
            {
                return BadRequest("해당 약어가 이 카테고리에 이미 존재합니다.");
            }

            var newAlias = new ChzzkCategoryAlias
            {
                CategoryId = categoryId,
                Alias = aliasName
            };

            db.ChzzkCategoryAliases.Add(newAlias);
            await db.SaveChangesAsync();

            return Ok(newAlias);
        }

        [HttpDelete("categories/{categoryId}/aliases/{aliasId}")]
        public async Task<IActionResult> DeleteCategoryAlias(string categoryId, int aliasId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var alias = await db.ChzzkCategoryAliases.FirstOrDefaultAsync(a => a.Id == aliasId && a.CategoryId == categoryId);
            if (alias == null)
            {
                return NotFound("해당 카테고리의 약어 데이터를 찾을 수 없습니다.");
            }

            db.ChzzkCategoryAliases.Remove(alias);
            await db.SaveChangesAsync();

            return Ok(new { message = "약어가 정상적으로 삭제되었습니다." });
        }
    }
}