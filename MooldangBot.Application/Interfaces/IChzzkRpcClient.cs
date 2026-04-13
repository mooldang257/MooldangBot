using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [?�시리스???�령 RPC]: 치�?�?게이?�웨?�로 명령??보내�?결과�??�기?�으�?RPC) ?�신?�는 ?�터?�이?�입?�다.
/// </summary>
public interface IChzzkRpcClient
{
    /// <summary>
    /// 치�?�?명령?��? 게이?�웨?�로 ?�신?�고 처리 결과�??�신?�니??
    /// </summary>
    /// <typeparam name="TResponse">?�상?�는 ?�답 ?�??(CommandResponseBase ?�속)</typeparam>
    /// <param name="command">?�행??명령??/param>
    /// <param name="timeout">?�답 ?��??�한 ?�간</param>
    /// <returns>처리 결과 ?�답</returns>
    Task<TResponse> SendCommandAsync<TResponse>(ChzzkCommandBase command, TimeSpan timeout) where TResponse : CommandResponseBase;
}

