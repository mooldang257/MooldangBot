using MooldangBot.Domain.Entities;

namespace MooldangBot.Contracts.Chzzk.Interfaces;

public interface IChzzkBotService
{
    Task SendReplyChatAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token);
    Task SendReplyNoticeAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token);
    Task UpdateTitleAsync(StreamerProfile profile, string newTitle, string senderUid, CancellationToken token);
    Task UpdateCategoryAsync(StreamerProfile profile, string category, string senderUid, string? categoryId = null, string? categoryType = null, CancellationToken token = default);
    Task RefreshChannelAsync(string chzzkUid);
    Task<string?> GetStreamerTokenAsync(StreamerProfile profile);
    Task EnsureConnectionAsync(string chzzkUid, bool forceFresh = false);
    void CleanupRecoveryLock(string chzzkUid);
}
