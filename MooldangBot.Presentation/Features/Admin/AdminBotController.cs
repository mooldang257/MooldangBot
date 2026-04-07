using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.DTOs;
using System.Text.Json;
using System.Text;
using MooldangBot.Application.Features.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using MooldangBot.Application.Common.Models;

namespace MooldangBot.Presentation.Features.Admin
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
    [Authorize(Roles = "master")] // 🔐 마스터 및 봇 전용 보안 강화
    // [v10.1] Primary Constructor 적용
    public class AdminBotController(
        IAppDbContext db, 
        IConfiguration configuration, 
        IServiceScopeFactory scopeFactory, 
        IChzzkCategorySyncService syncService, 
        IChzzkBotService chzzkBotService) : ControllerBase
    {
        // 2. 카테고리 동기화 상태 조회
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

        // 3. 카테고리 동기화 수동 시작
        [HttpPost("sync-categories")]
        public async Task<IActionResult> StartSync([FromBody] SyncRequest? req = null)
        {
            if (IChzzkCategorySyncService.IsRunning)
            {
                return BadRequest(Result<string>.Failure("이미 동기화가 진행 중입니다."));
            }

            var specificKeyword = req?.Keyword;

            if (!string.IsNullOrEmpty(specificKeyword))
            {
                // 단일 키워드 검색: 대기 후 즉시 결과 반환
                var results = await syncService.SearchAndSaveCategoryAsync(specificKeyword);

                return Ok(Result<object>.Success(new 
                { 
                    message = $"'{specificKeyword}' 키워드로 {results.Count}개의 카테고리를 동기화했습니다.",
                    results = results
                }));
            }

            // 백그라운드에서 실행되도록 Fire and Forget 방식으로 호출 (전체 동기화)
            _ = Task.Run(async () =>
            {
                await syncService.SyncCategoriesAsync(default);
            });

            return Ok(Result<object>.Success(new { message = "전체 카테고리 동기화를 시작했습니다." }));
        }

        // ==========================================
        // 카테고리 및 약어 관리 API 파트
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

            // 검색어가 없을 때는 최신 업데이트 순서대로 최대 100개만
            var results = await query.OrderByDescending(c => c.UpdatedAt).Take(100).ToListAsync();

            return Ok(Result<ListResponse<ChzzkCategory>>.Success(new ListResponse<ChzzkCategory>(results, results.Count)));
        }

        [HttpPost("categories/{categoryId}/aliases")]
        public async Task<IActionResult> AddCategoryAlias(string categoryId, [FromBody] AliasRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Alias))
                return BadRequest(Result<string>.Failure("약어(Alias)를 올바르게 입력해주세요."));

            using var scope = scopeFactory.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var category = await scopedDb.ChzzkCategories.FindAsync(categoryId);
            if (category == null)
                return NotFound(Result<string>.Failure("존재하지 않는 카테고리입니다."));

            var aliasName = request.Alias.Trim();

            // 중복 검사
            if (await scopedDb.ChzzkCategoryAliases.AnyAsync(a => a.CategoryId == categoryId && a.Alias == aliasName))
            {
                return BadRequest(Result<string>.Failure("해당 약어가 이 카테고리에 이미 존재합니다."));
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
                return NotFound(Result<string>.Failure("해당 카테고리의 약어 데이터를 찾을 수 없습니다."));
            }

            scopedDb.ChzzkCategoryAliases.Remove(alias);
            await scopedDb.SaveChangesAsync();

            return Ok(Result<object>.Success(new { success = true, message = "약어가 정상적으로 삭제되었습니다." }));
        }
    }
}