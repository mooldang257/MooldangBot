using MooldangBot.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces;

public interface IRouletteService
{
    Task<RouletteItem?> SpinRouletteAsync(string chzzkUid, int rouletteId, string viewerUid, string? viewerNickname = null, System.Threading.CancellationToken ct = default);
    Task<List<RouletteItem>> SpinRoulette10xAsync(string chzzkUid, int rouletteId, string viewerUid, string? viewerNickname = null, System.Threading.CancellationToken ct = default);
    Task<List<RouletteItem>> SpinRouletteMultiAsync(string chzzkUid, int rouletteId, string viewerUid, int count, string? viewerNickname = null, System.Threading.CancellationToken ct = default);

    /// <summary>
    /// 룰렛 결과를 지연 시간 후 채팅으로 전송합니다.
    /// </summary>
    Task SendDelayedChatResultAsync(string chzzkUid, int rouletteId, string itemName, string viewerUid, string? viewerNickname, System.Threading.CancellationToken ct = default);

    /// <summary>
    /// [v1.9.9] 오버레이로부터 완료 신호를 받아 결과를 즉시 전송하고 상태를 업데이트합니다.
    /// </summary>
    Task<bool> CompleteRouletteAsync(string spinId, System.Threading.CancellationToken ct = default);
}
