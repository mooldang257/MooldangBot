using MooldangBot.Contracts.Chzzk.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.DTOs;
using System.Text.Json;
using System.Text;
using MooldangBot.Application.Features.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using MooldangBot.Contracts.Common.Models;

namespace MooldangBot.Application.Controllers.Admin
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
    [Authorize(Roles = "master")] // ?�� 마스??�?�??�용 보안 강화
    // [v10.1] Primary Constructor ?�용
    public class AdminBotController(
        IServiceScopeFactory scopeFactory, 
        IChzzkCategorySyncService syncService) : ControllerBase
    {
        // 2. 카테고리 ?�기???�태 조회
        [HttpGet("sync-status")]
        public IActionResult GetSyncStatus()
        {
            return Ok(Result<object>.Success(new
            {
                isRunning = IChzzkCategorySyncService.IsRunning,
                lastRunTime = IChzzkCategorySyncService.LastRunTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                lastResult = IChzzkCategorySyncService.LastResult,
                addedCount = IChzzkCategorySyncService.LastAddedCount,
                updatedCount = IChzzkCategorySyncService.LastUpdatedCount
            }));
        }

        // 3. 카테고리 ?�기???�동 ?�작
        [HttpPost("sync-categories")]
        public async Task<IActionResult> StartSync([FromBody] SyncRequest? req = null)
        {
            if (IChzzkCategorySyncService.IsRunning)
            {
                return BadRequest(Result<string>.Failure("?��? ?�기?��? 진행 중입?�다."));
            }

            var specificKeyword = req?.Keyword;

            if (!string.IsNullOrEmpty(specificKeyword))
            {
                // ?�일 ?�워??검?? ?��???즉시 결과 반환
                var results = await syncService.SearchAndSaveCategoryAsync(specificKeyword);

                return Ok(Result<object>.Success(new 
                { 
                    message = $"'{specificKeyword}' ?�워?�로 {results.Count}개의 카테고리�??�기?�했?�니??",
                    results = results
                }));
            }

            // 백그?�운?�에???�행?�도�?Fire and Forget 방식?�로 ?�출 (?�체 ?�기??
            _ = Task.Run(async () =>
            {
                await syncService.SyncCategoriesAsync(default);
            });

            return Ok(Result<object>.Success(new { message = "?�체 카테고리 ?�기?��? ?�작?�습?�다." }));
        }

        // ==========================================
        // 카테고리 �??�어 관�?API ?�트
        // ==========================================

        [HttpGet("categories")]
        public async Task<IActionResult> SearchCategories([FromQuery] string? search)
        {
            using var scope = scopeFactory.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var query = scopedDb.ChzzkCategories.Include(c => c.Aliases).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(c => c.CategoryValue.Contains(s) || c.CategoryId.Contains(s) || c.Aliases.Any(a => a.Alias.Contains(s)));
            }

            // 검?�어가 ?�을 ?�는 최신 ?�데?�트 ?�서?��?최�? 100개만
            var results = await query.OrderByDescending(c => c.UpdatedAt).Take(100).ToListAsync();

            return Ok(Result<ListResponse<ChzzkCategory>>.Success(new ListResponse<ChzzkCategory>(results, results.Count)));
        }

        [HttpPost("categories/{categoryId}/aliases")]
        public async Task<IActionResult> AddCategoryAlias(string categoryId, [FromBody] AliasRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Alias))
                return BadRequest(Result<string>.Failure("?�어(Alias)�??�바르게 ?�력?�주?�요."));

            using var scope = scopeFactory.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var category = await scopedDb.ChzzkCategories.FindAsync(categoryId);
            if (category == null)
                return NotFound(Result<string>.Failure("존재?��? ?�는 카테고리?�니??"));

            var aliasName = request.Alias.Trim();

            // 중복 검??
            if (await scopedDb.ChzzkCategoryAliases.AnyAsync(a => a.CategoryId == categoryId && a.Alias == aliasName))
            {
                return BadRequest(Result<string>.Failure("?�당 ?�어가 ??카테고리???��? 존재?�니??"));
            }

            var newAlias = new ChzzkCategoryAlias
            {
                CategoryId = categoryId,
                Alias = aliasName
            };

            scopedDb.ChzzkCategoryAliases.Add(newAlias);
            await scopedDb.SaveChangesAsync();

            return Ok(Result<ChzzkCategoryAlias>.Success(newAlias));
        }

        [HttpDelete("categories/{categoryId}/aliases/{aliasId}")]
        public async Task<IActionResult> DeleteCategoryAlias(string categoryId, int aliasId)
        {
            using var scope = scopeFactory.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var alias = await scopedDb.ChzzkCategoryAliases.FirstOrDefaultAsync(a => a.Id == aliasId && a.CategoryId == categoryId);
            if (alias == null)
            {
                return NotFound(Result<string>.Failure("?�당 카테고리???�어 ?�이?��? 찾을 ???�습?�다."));
            }

            scopedDb.ChzzkCategoryAliases.Remove(alias);
            await scopedDb.SaveChangesAsync();

            return Ok(Result<object>.Success(new { success = true, message = "?�어가 ?�상?�으�???��?�었?�니??" }));
        }
    }
}
