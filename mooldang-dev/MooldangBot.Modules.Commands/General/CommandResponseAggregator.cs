using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using System.Text;

namespace MooldangBot.Modules.Commands.General;

/// <summary>
/// [하모니의 집약]: 실행된 여러 명령어의 응답을 수집하고 한 번에 전송합니다.
/// </summary>
public class CommandResponseAggregator : ICommandResponseAggregator
{
    private readonly List<string> _responses = new();
    private readonly IChzzkBotService _botService;

    public CommandResponseAggregator(IChzzkBotService botService)
    {
        _botService = botService;
    }

    public void AddResponse(string response)
    {
        if (!string.IsNullOrWhiteSpace(response))
        {
            _responses.Add(response);
        }
    }

    public async Task<string> AggregateAndFlushAsync(CancellationToken ct = default)
    {
        if (!_responses.Any()) return string.Empty;

        var combined = string.Join("\n", _responses);
        _responses.Clear();
        
        return combined;
    }

    public bool HasResponses => _responses.Any();
}
