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

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Client-Id", clientId);
        client.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
        // 카테고리 검색 API는 사용자 토큰(Bearer)을 사용하지 않는 API입니다.
        // client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.ChzzkAccessToken);

        int addedCount = 0;
        int updatedCount = 0;

        foreach (var keyword in keywords)
        {
            if (ct.IsCancellationRequested) break;

            try 
            {
                var response = await client.GetAsync($"https://openapi.chzzk.naver.com/open/v1/categories/search?query={Uri.EscapeDataString(keyword)}&size=50", ct);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning($"⚠️ [배치] 카테고리 API 400 에러 - Keyword: {keyword}, StatusCode: {response.StatusCode}, Content: {errorContent}");
                    continue;
                }

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

    public async Task<List<ChzzkCategory>> SearchAndSaveCategoryAsync(string keyword, CancellationToken ct = default)
    {
        var results = new List<ChzzkCategory>();
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var clientId = await db.SystemSettings.Where(s => s.KeyName == "ChzzkClientId").Select(s => s.KeyValue).FirstOrDefaultAsync(ct);
        var clientSecret = await db.SystemSettings.Where(s => s.KeyName == "ChzzkClientSecret").Select(s => s.KeyValue).FirstOrDefaultAsync(ct);
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return results;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Client-Id", clientId);
        client.DefaultRequestHeaders.Add("Client-Secret", clientSecret);

        try 
        {
            var response = await client.GetAsync($"https://openapi.chzzk.naver.com/open/v1/categories/search?query={Uri.EscapeDataString(keyword)}&size=50", ct);
            if (!response.IsSuccessStatusCode) return results;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("content", out var contentNode) && contentNode.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
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
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"⚠️ [수동] 키워드 '{keyword}' 실시간 검색 중 오류: {ex.Message}");
        }
        
        return results;
    }
}
