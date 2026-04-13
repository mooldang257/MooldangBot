using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Services.Philosophy;

/// <summary>
/// [오시리스의 규율]: 시스템 정합성과 윤리적 가이드를 검증하는 실체입니다.
/// </summary>
public class RegulationService : IRegulationService
{
    private readonly ILogger<RegulationService> _logger;

    public RegulationService(ILogger<RegulationService> _logger)
    {
        this._logger = _logger;
    }

    public async Task<(bool IsValid, string Message)> ValidateRegulationAsync(string input, string persona)
    {
        // [오시리스의 심판]: 입력값의 정합성을 검증합니다.
        if (string.IsNullOrWhiteSpace(input))
        {
            return (false, "[오시리스의 거절]: 입력된 파동이 존재하지 않습니다.");
        }

        // 특정 페르소나에 따른 특수 규율 (예시)
        if (persona == "Osiris" && input.Contains("속임수"))
        {
            _logger.LogWarning($"[오시리스의 경고] 부적절한 파동 감지: {input}");
            return (false, "[오시리스의 거절]: 실험의 정합성을 해치는 파동이 감지되었습니다.");
        }

        return await Task.FromResult((true, "[오시리스의 허가]: 파동이 규율에 적합합니다."));
    }
}
