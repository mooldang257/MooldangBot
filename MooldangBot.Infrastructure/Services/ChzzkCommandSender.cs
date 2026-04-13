using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Contracts.Chzzk.Models.Commands;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 전령]: ISendEndpointProvider를 사용하여 특정 큐로 명령을 발송하는 구현체입니다.
/// </summary>
public class ChzzkCommandSender(
    ISendEndpointProvider sendEndpointProvider, 
    ILogger<ChzzkCommandSender> logger) : IChzzkCommandSender
{
    public async Task SendAsync(ChzzkCommandBase command, CancellationToken ct = default)
    {
        logger.LogDebug("📤 [Fire&Forget] 명령 송신: {Type} (ID: {Id})", command.GetType().Name, command.MessageId);
        
        // [시니어 가이드]: EndpointConvention에 등록된 주소(chzzk-commands-rpc)로 
        // 1:1 다이렉트 송신을 수행하여 브로드캐스트 부하를 방지합니다.
        await sendEndpointProvider.Send(command, command.GetType(), ct);
    }
}
