using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [하모니의 전략]: FeatureType에 따른 개별 명령어 실행 로직을 정의합니다.
/// </summary>
public interface ICommandFeatureStrategy
{
    /// <summary>
    /// 이 전략이 처리할 FeatureType (예: Song, Roulette)
    /// </summary>
    string FeatureType { get; }

    /// <summary>
    /// 명령어를 실제로 실행합니다.
    /// </summary>
    Task ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct);
}
