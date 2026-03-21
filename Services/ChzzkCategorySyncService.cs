using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;

namespace MooldangAPI.Services;

public class ChzzkCategorySyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChzzkCategorySyncService> _logger;

    public ChzzkCategorySyncService(IServiceProvider serviceProvider, ILogger<ChzzkCategorySyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // 상태 추적용 정적 필드
    public static bool IsRunning { get; private set; }
    public static DateTime? LastRunTime { get; private set; }
    public static string LastResult { get; private set; } = "Not started";
    public static int LastAddedCount { get; private set; }
    public static int LastUpdatedCount { get; private set; }

    public async Task SyncCategoriesAsync(CancellationToken ct = default)
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
        
        // 1. API 키 및 토큰 확보
        var clientId = await db.SystemSettings.Where(s => s.KeyName == "ChzzkClientId").Select(s => s.KeyValue).FirstOrDefaultAsync(ct);
        var clientSecret = await db.SystemSettings.Where(s => s.KeyName == "ChzzkClientSecret").Select(s => s.KeyValue).FirstOrDefaultAsync(ct);
        
        // 검색에 필요한 베어러 토큰 (아무 스트리머나 유효한 토큰 하나 사용)
        var profile = await db.StreamerProfiles.Where(p => !string.IsNullOrEmpty(p.ChzzkAccessToken)).FirstOrDefaultAsync(ct);

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || profile == null)
        {
            _logger.LogError("❌ [배치] 카테고리 동기화 실패: API 키 또는 유효한 스트리머 토큰이 없습니다.");
            return;
        }

        // 2. 수집할 키워드 정의 (한글 초성, 알파벳, 주요 카테고리)
        var keywords = new List<string> { "ㄱ", "ㄴ", "ㄷ", "ㄹ", "ㅁ", "ㅂ", "ㅅ", "ㅇ", "ㅈ", "ㅊ", "ㅋ", "ㅌ", "ㅍ", "ㅎ" };
        keywords.AddRange("abcdefghijklmnopqrstuvwxyz".Select(c => c.ToString()));
        keywords.AddRange(new[] { "League", "Valorant", "Minecraft", "Maple", "Lost Ark", "Tekken", "Talk", "Just Chatting", "리그", "마인크" });

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Client-Id", clientId);
        client.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.ChzzkAccessToken);

        int addedCount = 0;
        int updatedCount = 0;

        foreach (var keyword in keywords)
        {
            if (ct.IsCancellationRequested) break;

            try 
            {
                var response = await client.GetAsync($"https://openapi.chzzk.naver.com/open/v1/categories/search?keyword={Uri.EscapeDataString(keyword)}&size=50", ct);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                
                if (!doc.RootElement.TryGetProperty("content", out var contentNode)) continue;
                if (!contentNode.TryGetProperty("data", out var dataArray)) continue;

                if (dataArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var categoryId = item.GetProperty("categoryId").GetString();
                        if (string.IsNullOrEmpty(categoryId)) continue;

                        var categoryValue = item.GetProperty("categoryValue").GetString() ?? "";
                        var categoryType = item.GetProperty("categoryType").GetString() ?? "";
                        var posterUrl = item.TryGetProperty("posterImageUrl", out var p) ? p.GetString() : null;

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
}
