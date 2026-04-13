using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 전령 RPC 실구현체]: MassTransit의 IRequestClient를 사용하여 게이트웨이와 통신합니다.
/// (v4.0): 수동 채널 관리 및 타임아웃 로직이 MassTransit 인프라로 통합되었습니다.
/// </summary>
public class ChzzkRpcClient(
    IBus bus, // MassTransit Bus를 통해 런타임에 RequestClient 생성
    ILogger<ChzzkRpcClient> logger) : IChzzkRpcClient
{
    public async Task<TResponse> SendCommandAsync<TResponse>(ChzzkCommandBase command, TimeSpan timeout) 
        where TResponse : CommandResponseBase
    {
        try
        {
            logger.LogInformation("🚀 [RPC 명령 송신] 유형: {Type}, ID: {Id}", command.GetType().Name, command.MessageId);

            // [오시리스의 영혼]: MassTransit Request Client 생성 및 요청 발송
            // TResponse는 StandardCommandResponse 등 구체적인 타입을 기대합니다.
            var client = bus.CreateRequestClient<ChzzkCommandBase>(timeout);
            
            var response = await client.GetResponse<TResponse>(command);

            logger.LogDebug("📥 [RPC 응답 수신] ID: {Id}, Status: Success", command.MessageId);
            return response.Message;
        }
        catch (RequestTimeoutException)
        {
            logger.LogError("⚠️ [RPC 타임아웃] 명령 {Id}에 대한 응답이 {Timeout}초 내에 오지 않았습니다.", command.MessageId, timeout.TotalSeconds);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [RPC 통신 오류] {Id} 처리 중 예외 발생", command.MessageId);
            throw;
        }
    }
}
