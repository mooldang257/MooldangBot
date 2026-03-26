using MooldangBot.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

public interface IRouletteService
{
    Task<RouletteItem?> SpinRouletteAsync(string chzzkUid, int rouletteId, string? viewerNickname = null);
    Task<List<RouletteItem>> SpinRoulette10xAsync(string chzzkUid, int rouletteId, string? viewerNickname = null);
    Task<List<RouletteItem>> SpinRouletteMultiAsync(string chzzkUid, int rouletteId, int count, string? viewerNickname = null);

    /// <summary>
    /// 룰렛 결과를 지연 시간 후 채팅으로 전송합니다.
    /// </summary>
    Task SendDelayedChatResultAsync(string chzzkUid, int rouletteId, string itemName, string? viewerNickname);
}
