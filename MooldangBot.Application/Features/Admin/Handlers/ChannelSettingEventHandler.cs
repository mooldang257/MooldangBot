using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Admin.Handlers;

public class ChannelSettingEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChannelSettingEventHandler> _logger;
    private readonly IChzzkApiClient _chzzkApi;
    private readonly IChzzkBotService _botService;

    // 💡 [카테고리 사전]: 사용자가 입력하는 단축어 -> 검색용 키워드
    private static readonly Dictionary<string, string> CategorySearchAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        { "저챗", "talk" },
        { "소통", "talk" },
        { "노가리", "talk" },
        { "먹방", "먹방/쿡방" },
        { "노래", "음악/노래" },
        { "종겜", "종합 게임" },
        { "롤", "리그 오브 레전드" },
        { "발로", "발로란트" },
        { "배그", "BATTLEGROUNDS" },
        { "마크", "Minecraft" },
        { "메", "메이플스토리" },
        { "로아", "로스트아크" },
        { "철권", "철권 8" }
    };

    public ChannelSettingEventHandler(ILogger<ChannelSettingEventHandler> logger, IServiceProvider serviceProvider, IChzzkApiClient chzzkApi, IChzzkBotService botService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _chzzkApi = chzzkApi;
        _botService = botService;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        string msg = notification.Message;
        string nickname = notification.Username;
        bool isMaster = notification.SenderId == notification.Profile.ChzzkUid || notification.SenderId == "ca98875d5e0edf02776047fbc70f5449";
        
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
            
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var aliasObj = await db.ChzzkCategoryAliases.Include(a => a.Category).FirstOrDefaultAsync(a => a.Alias == inputKeyword, cancellationToken);
            
            string searchKeyword = aliasObj?.Category?.CategoryValue ?? (CategorySearchAlias.TryGetValue(inputKeyword, out var hardcoded) ? hardcoded : inputKeyword);
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
            var updateData = new { defaultLiveTitle = newTitle ?? "" };
            bool success = await _chzzkApi.UpdateLiveSettingAsync(req.Profile.ChzzkAccessToken ?? "", updateData);

            if (success)
            {
                _logger.LogInformation($"✨ [{req.Profile.ChzzkUid}] 방제 변경 완료!");
                await SendReplyChatAsync(req, "✅ 방송 제목이 변경되었습니다.", token);
            }
            else
            {
                _logger.LogError($"❌ [방제 변경 실패] {req.Profile.ChzzkUid}");
                await SendReplyChatAsync(req, "❌ 방제 변경에 실패했습니다. 권한을 확인해주세요.", token);
            }
        }
        catch (Exception ex) { _logger.LogError($"❌ [API 통신 오류] {ex.Message}"); }
    }

    private async Task<(string Type, string Id, string Name)?> SearchChzzkCategoryAsync(ChatMessageReceivedEvent req, string keyword, CancellationToken token)
    {
        try
        {
            var searchResult = await _chzzkApi.SearchCategoryAsync(keyword);

            if (searchResult != null && searchResult.Code == 200 && searchResult.Content?.Data?.Count > 0)
            {
                var firstResult = searchResult.Content.Data[0];
                return (firstResult.CategoryType, firstResult.CategoryId, firstResult.CategoryValue);
            }
        }
        catch (Exception ex) { _logger.LogError($"❌ [카테고리 검색 오류] {ex.Message}"); }
        return null;
    }

    private async Task UpdateChannelCategoryAsync(ChatMessageReceivedEvent req, string type, string id, string name, CancellationToken token)
    {
        try
        {
            var updateData = new { categoryType = type, categoryId = id };
            bool success = await _chzzkApi.UpdateLiveSettingAsync(req.Profile.ChzzkAccessToken ?? "", updateData);

            if (success)
            {
                await SendReplyChatAsync(req, $"✅ 카테고리가 [{name}](으)로 변경되었습니다.", token);
            }
            else
            {
                _logger.LogError($"❌ [카테고리 변경 실패] {req.Profile.ChzzkUid}");
                await SendReplyChatAsync(req, "❌ 카테고리 변경에 실패했습니다.", token);
            }
        }
        catch (Exception ex) { _logger.LogError($"❌ {ex.Message}"); }
    }

    private async Task SendReplyChatAsync(ChatMessageReceivedEvent req, string message, CancellationToken token)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        var profile = await db.StreamerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.ChzzkUid == req.Profile.ChzzkUid, token);
        if (profile != null)
        {
            await _botService.SendReplyChatAsync(profile, message, token);
        }
    }
}
