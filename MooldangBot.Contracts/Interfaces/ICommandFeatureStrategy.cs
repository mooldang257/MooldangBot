using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;

namespace MooldangBot.Contracts.Interfaces;

public interface ICommandFeatureStrategy
{
    string FeatureType { get; }
    Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent_Legacy notification, UnifiedCommand command, CancellationToken ct);
}
