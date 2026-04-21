using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;

namespace MooldangBot.Modules.Commands.Abstractions;

public interface ICommandFeatureStrategy
{
    string FeatureType { get; }
    Task<CommandExecutionResult> ExecuteAsync(ChatMessageEvent notification, UnifiedCommand command, CancellationToken ct);
}
