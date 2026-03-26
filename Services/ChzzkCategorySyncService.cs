using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Infrastructure.Persistence;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangAPI.ApiClients;

namespace MooldangAPI.Services;

public class ChzzkCategorySyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChzzkCategorySyncService> _logger;

    private readonly ChzzkApiClient _chzzkApi;

    public ChzzkCategorySyncService(IServiceProvider serviceProvider, ILogger<ChzzkCategorySyncService> logger, ChzzkApiClient chzzkApi)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _chzzkApi = chzzkApi;
    }

    // 상태 추적용 정적 필드
    public static bool IsRunning { get; private set; }
    public static DateTime? LastRunTime { get; private set; }
    public static string LastResult { get; private set; } = "Not started";
    public static int LastAddedCount { get; private set; }
    public static int LastUpdatedCount { get; private set; }

    public async Task SyncCategoriesAsync(string? specificKeyword = null, CancellationToken ct = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("⚠️ [배치] 카테고리 동기화가 이미 실행 중입니다.");
            return;
        }

        try
        {
            IsRunning = true;
            LastRunTime = DateTime.Now;
            LastResult = "Running...";
            LastAddedCount = 0;
            LastUpdatedCount = 0;

            _logger.LogInformation("🔄 [배치] 치지직 카테고리 동기화 시작...");

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 2. 수집할 키워드 정의
        var keywords = new List<string>();
        
        if (!string.IsNullOrEmpty(specificKeyword))
        {
            keywords.Add(specificKeyword); // 사용자가 입력한 특정 키워드 1개만 검색
            _logger.LogInformation($"🔄 [배치] '{specificKeyword}' 단일 키워드로 카테고리 동기화를 시작합니다.");
        }
        else
        {
            keywords.AddRange(new[] { "ㄱ", "ㄴ", "ㄷ", "ㄹ", "ㅁ", "ㅂ", "ㅅ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ" });
            keywords.AddRange("abcdefghijklmnopqrstuvwxyz".Select(c => c.ToString()));
            keywords.AddRange(new[] { 
                "League", "Valorant", "Minecraft", "Maple", "Lost Ark", "Tekken", "Talk", "Just Chatting",
                "리그 오브 레전드", "리그오브레전드", "롤", "발로란트", "마인크래프트", "마인크", "메이플스토리", "메이플",
                "로스트아크", "로아", "철권", "토크", "저스트채팅", "종합게임", "종겜", "야외방송", "먹방", "ASMR",
                "음악", "노래", "Music", "라디오", "아이온", "AION", "VRChat", "로블록스", "원신", "이터널 리턴",
                "팰월드", "Palworld", "스타크래프트", "배틀그라운드", "PUBG", "서든어택", "던전앤파이터", "오버워치",
                "오버워치 2", "디아블로", "리니지", "에이펙스", "Apex", "GTA", "파이널판타지", "월드 오브 워크래프트",
                "스타듀밸리", "동물의 숲", "포켓몬", "명조", "붕괴", "버추얼", "캠카방", "요리"
            });
        }

        int addedCount = 0;
        int updatedCount = 0;

        foreach (var keyword in keywords)
        {
            if (ct.IsCancellationRequested) break;

            try 
            {
                var response = await _chzzkApi.SearchCategoryAsync(keyword);
                if (response == null || response.Code != 200 || response.Content?.Data == null)
                {
                    _logger.LogWarning($"⚠️ [배치] 카테고리 검색 결과 없음 또는 에러 - Keyword: {keyword}");
                    continue;
                }

                foreach (var item in response.Content.Data)
                {
                    var categoryId = item.CategoryId;
                    if (string.IsNullOrEmpty(categoryId)) continue;

                    var categoryValue = item.CategoryValue;
                    var categoryType = item.CategoryType;
                    var posterUrl = ""; // 모델에 따라 필요시 확장 가능

                    var existing = await db.ChzzkCategories.FirstOrDefaultAsync(c => c.CategoryId == categoryId, ct);
                    if (existing == null)
                    {
                        db.ChzzkCategories.Add(new ChzzkCategory
                        {
                            CategoryId = categoryId,
                            CategoryValue = categoryValue,
                            CategoryType = categoryType,
                            PosterImageUrl = posterUrl,
                            UpdatedAt = DateTime.UtcNow
                        });
                        addedCount++;
                    }
                    else
                    {
                        existing.CategoryValue = categoryValue;
                        existing.CategoryType = categoryType;
                        existing.PosterImageUrl = posterUrl;
                        existing.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                    }
                }
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ [배치] 키워드 '{keyword}' 검색 중 오류: {ex.Message}");
            }
            
            await Task.Delay(500, ct); // 과도한 API 호출 방지
        }

        LastAddedCount = addedCount;
        LastUpdatedCount = updatedCount;
        LastResult = $"Success (New: {addedCount}, Updated: {updatedCount})";
        _logger.LogInformation($"✅ [배치] 카테고리 동기화 완료: {LastResult}");
    }
    catch (Exception ex)
    {
        LastResult = $"Failed: {ex.Message}";
        _logger.LogError($"❌ [배치] 카테고리 동기화 중 오류: {ex.Message}");
    }
    finally
    {
        IsRunning = false;
    }
    }

    public async Task<List<ChzzkCategory>> SearchAndSaveCategoryAsync(string keyword, CancellationToken ct = default)
    {
        var results = new List<ChzzkCategory>();
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        try 
        {
            var response = await _chzzkApi.SearchCategoryAsync(keyword);
            if (response == null || response.Code != 200 || response.Content?.Data == null) return results;

            foreach (var item in response.Content.Data)
            {
                var categoryId = item.CategoryId;
                if (string.IsNullOrEmpty(categoryId)) continue;

                var categoryValue = item.CategoryValue;
                var categoryType = item.CategoryType;
                var posterUrl = "";

                var existing = await db.ChzzkCategories.FirstOrDefaultAsync(c => c.CategoryId == categoryId, ct);
                if (existing == null)
                {
                    var newCategory = new ChzzkCategory
                    {
                        CategoryId = categoryId,
                        CategoryValue = categoryValue,
                        CategoryType = categoryType,
                        PosterImageUrl = posterUrl,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.ChzzkCategories.Add(newCategory);
                    results.Add(newCategory);
                }
                else
                {
                    existing.CategoryValue = categoryValue;
                    existing.CategoryType = categoryType;
                    existing.PosterImageUrl = posterUrl;
                    existing.UpdatedAt = DateTime.UtcNow;
                    results.Add(existing);
                }
            }
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"⚠️ [수동] 키워드 '{keyword}' 실시간 검색 중 오류: {ex.Message}");
        }
        
        return results;
    }
}
