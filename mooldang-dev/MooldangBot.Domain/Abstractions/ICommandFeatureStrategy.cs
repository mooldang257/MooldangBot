using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;

namespace MooldangBot.Domain.Abstractions;

/// <summary>
/// [오시리스의 지팡이]: 특정 기능(Omakase, Point 등)을 처리하기 위한 전략 인터페이스입니다.
/// (v15.2): 모듈 간 순환 참조를 방지하기 위해 Domain 레이어로 이동되었습니다.
/// </summary>
public interface ICommandFeatureStrategy
{
    string FeatureType { get; }
    Task<CommandExecutionResult> ExecuteAsync(ChatMessageEvent notification, FuncCmdUnified command, CancellationToken ct);
}
