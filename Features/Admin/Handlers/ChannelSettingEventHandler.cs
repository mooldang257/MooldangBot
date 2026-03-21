using MediatR;
using MooldangAPI.Features.Chat.Events;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MooldangAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace MooldangAPI.Features.Admin.Handlers;

public class ChannelSettingEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly ILogger<ChannelSettingEventHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ChannelSettingEventHandler(ILogger<ChannelSettingEventHandler> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        string msg = notification.Message;
        string nickname = notification.Username;
        bool isMaster = notification.SenderId == "ca98875d5e0edf02776047fbc70f5449";
        
        bool isAuthorized = isMaster || notification.UserRole == "streamer" || notification.UserRole == "manager";

        if (msg.StartsWith("!방제 ") && isAuthorized)
        {
            string newTitle = msg.Substring("!방제 ".Length).Trim();
            _logger.LogInformation($"🛠️ [방제 변경 요청 포착] {nickname}님 -> {newTitle}");
            await UpdateChannelInfoAsync(notification, newTitle, cancellationToken);
        }
        else if (msg.StartsWith("!카테고리 ") && isAuthorized)
        {
            string inputKeyword = msg.Substring("!카테고리 ".Length).Trim();
            _logger.LogInformation($"🛠️ [카테고리 변경 요청] {nickname}님 -> 원본: {inputKeyword}");
            
            // DB에서 약어 조회
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var aliasObj = await db.ChzzkCategoryAliases.Include(a => a.Category).FirstOrDefaultAsync(a => a.Alias == inputKeyword, cancellationToken);
            
            string searchKeyword = aliasObj?.Category?.CategoryValue ?? inputKeyword;
            var categoryInfo = await SearchChzzkCategoryAsync(notification, searchKeyword, cancellationToken);

            if (categoryInfo != null)
            {
                await UpdateChannelCategoryAsync(notification, categoryInfo.Value.Type, categoryInfo.Value.Id, categoryInfo.Value.Name, cancellationToken);
            }
            else
            {
                _logger.LogWarning($"⚠️ 카테고리 검색 실패: {searchKeyword}");
                await SendReplyChatAsync(notification, $"❌ '{searchKeyword}'(으)로 검색되는 치지직 카테고리가 없습니다.", cancellationToken);
            }
        }
    }

    private async Task UpdateChannelInfoAsync(ChatMessageReceivedEvent req, string? newTitle, CancellationToken token)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Client-Id", req.ClientId);
            httpClient.DefaultRequestHeaders.Add("Client-Secret", req.ClientSecret);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.Profile.ChzzkAccessToken);

            var updateData = new Dictionary<string, object> { { "defaultLiveTitle", newTitle ?? "" } };
            var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");
            var response = await httpClient.PatchAsync("https://openapi.chzzk.naver.com/open/v1/lives/setting", content, token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"✨ [{req.Profile.ChzzkUid}] 방제 변경 완료!");
                await SendReplyChatAsync(req, "✅ 방송 설정이 변경되었습니다.", token);
            }
        }
        catch (Exception ex) { _logger.LogError($"❌ [정식 API 통신 오류] {ex.Message}"); }
    }

    private async Task<(string Type, string Id, string Name)?> SearchChzzkCategoryAsync(ChatMessageReceivedEvent req, string keyword, CancellationToken token)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Client-Id", req.ClientId);
            httpClient.DefaultRequestHeaders.Add("Client-Secret", req.ClientSecret);
            // 퍼블릭 카테고리 검색 API는 사용자 토큰 불필요
            // httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.Profile.ChzzkAccessToken);

            string encodedQuery = Uri.EscapeDataString(keyword);
            var response = await httpClient.GetAsync($"https://openapi.chzzk.naver.com/open/v1/categories/search?query={encodedQuery}&size=30", token);

            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync(token);
                using var doc = JsonDocument.Parse(jsonResult);
                var dataArray = doc.RootElement.GetProperty("content").GetProperty("data");

                if (dataArray.ValueKind == JsonValueKind.Array && dataArray.GetArrayLength() > 0)
                {
                    var firstResult = dataArray[0];
                    return (firstResult.GetProperty("categoryType").GetString() ?? "ETC",
                            firstResult.GetProperty("categoryId").GetString() ?? "",
                            firstResult.GetProperty("categoryValue").GetString() ?? keyword);
                }
            }
        }
        catch (Exception ex) { _logger.LogError($"❌ [카테고리 검색 오류] {ex.Message}"); }
        return null;
    }

    private async Task UpdateChannelCategoryAsync(ChatMessageReceivedEvent req, string type, string id, string name, CancellationToken token)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Client-Id", req.ClientId);
            httpClient.DefaultRequestHeaders.Add("Client-Secret", req.ClientSecret);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.Profile.ChzzkAccessToken);

            var updateData = new Dictionary<string, object> { { "categoryType", type }, { "categoryId", id } };
            var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");
            var response = await httpClient.PatchAsync("https://openapi.chzzk.naver.com/open/v1/lives/setting", content, token);

            if (response.IsSuccessStatusCode)
            {
                await SendReplyChatAsync(req, $"✅ 카테고리가 [{name}](으)로 변경되었습니다.", token);
            }
        }
        catch (Exception ex) { _logger.LogError($"❌ {ex.Message}"); }
    }

    private async Task SendReplyChatAsync(ChatMessageReceivedEvent req, string message, CancellationToken token)
    {
        using var replyClient = new HttpClient();
        replyClient.DefaultRequestHeaders.Add("Client-Id", req.ClientId);
        replyClient.DefaultRequestHeaders.Add("Client-Secret", req.ClientSecret);
        replyClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.Profile.ChzzkAccessToken);

        string replyText = "\u200B" + message; 
        if (replyText.Length > 500) replyText = replyText.Substring(0, 497) + "...";

        var replyReq = new { message = replyText };
        await replyClient.PostAsync("https://openapi.chzzk.naver.com/open/v1/chats/send",
            new StringContent(JsonSerializer.Serialize(replyReq), Encoding.UTF8, "application/json"), token);
    }
}
