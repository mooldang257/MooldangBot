namespace MooldangBot.Contracts.Interfaces;

/// <summary>
/// [오시리스의 규율]: 시스템 정합성과 윤리적 가이드를 검증하는 인터페이스입니다.
/// </summary>
public interface IRegulationService
{
    /// <summary>
    /// 입력된 파동(요청)이 규율에 적합한지 검증합니다.
    /// </summary>
    Task<(bool IsValid, string Message)> ValidateRegulationAsync(string input, string persona);
}
