namespace MooldangBot.Contracts.Common.Interfaces;

public interface IDynamicQueryEngine
{
    Task<string> ProcessMessageAsync(string template, string streamerChzzkUid, string? senderId = null, string? senderNickname = null);
}
