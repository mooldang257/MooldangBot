using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 인장]: 결과를 기다리지 않고 치지직 명령을 비동기로 송신하는 인터페이스입니다.
/// </summary>
public interface IChzzkCommandSender
{
    /// <summary>
    /// 치지직 명령을 게이트웨이로 송신합니다. (Fire & Forget)
    /// </summary>
    /// <param name="command">송신할 명령 객체</param>
    /// <param name="ct">취소 토큰</param>
    Task SendAsync(ChzzkCommandBase command, CancellationToken ct = default);
}
