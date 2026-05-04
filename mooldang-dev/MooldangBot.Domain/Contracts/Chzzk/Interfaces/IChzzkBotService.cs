using MooldangBot.Domain.Entities;

namespace MooldangBot.Domain.Contracts.Chzzk.Interfaces;

public interface IChzzkBotService
{
    Task SendReplyChatAsync(CoreStreamerProfiles profile, string message, string viewerUid, CancellationToken token);
    Task SendReplyNoticeAsync(CoreStreamerProfiles profile, string message, string viewerUid, CancellationToken token);
    Task UpdateTitleAsync(CoreStreamerProfiles profile, string newTitle, string senderUid, CancellationToken token);
    Task UpdateCategoryAsync(CoreStreamerProfiles profile, string category, string senderUid, string? categoryId = null, string? categoryType = null, CancellationToken token = default);
    Task RefreshChannelAsync(string chzzkUid);
    Task<string?> GetStreamerTokenAsync(CoreStreamerProfiles profile);
    Task EnsureConnectionAsync(string chzzkUid, bool forceFresh = false);
    void CleanupRecoveryLock(string chzzkUid);
}
