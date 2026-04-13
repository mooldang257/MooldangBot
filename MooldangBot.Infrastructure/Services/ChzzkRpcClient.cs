using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 전령 RPC 실구현체]: MassTransit의 IRequestClient를 사용하여 게이트웨이와 통신합니다.
/// (v4.0): 수동 채널 관리 및 타임아웃 로직이 MassTransit 인프라로 통합되었습니다.
/// </summary>
public class ChzzkRpcClient(
    IServiceProvider serviceProvider, // [물멍 가이드]: 동적 클라이언트 조회를 위해 주입
    ILogger<ChzzkRpcClient> logger) : IChzzkRpcClient
{
    public async Task<TResponse> SendCommandAsync<TResponse>(ChzzkCommandBase command, TimeSpan timeout) 
        where TResponse : CommandResponseBase
    {
        try
        {
            // 1. 런타임에 명령 객체의 구체적 타입(예: ReconnectCommand)을 식별합니다.
            var requestType = command.GetType();
            logger.LogInformation("🚀 [RPC 명령 송신] 유형: {Type}, ID: {Id}", requestType.Name, command.MessageId);

            // 2. 해당 타입에 맞는 IRequestClient<T>의 제네릭 타입을 런타임에 생성합니다.
            // [시니어 팁]: MassTransit은 제네릭 타입 파라미터를 기준으로 URN을 생성하므로 이 과정이 필수적입니다.
            var requestClientType = typeof(IRequestClient<>).MakeGenericType(requestType);

            // 3. DI 컨테이너에서 해당 클라이언트를 꺼내옵니다.
            var client = serviceProvider.GetRequiredService(requestClientType);
            
            // 4. dynamic 키워드를 사용해 복잡한 리플렉션 없이 GetResponse<T> 메서드를 호출합니다.
            dynamic dynamicClient = client;
            dynamic dynamicCommand = command;
            
            // [오시리스의 영혼]: 실제 통신 발생. TResponse는 StandardCommandResponse 등을 기대합니다.
            var response = await dynamicClient.GetResponse<TResponse>(dynamicCommand, cancellationToken: default(CancellationToken));

            logger.LogDebug("📥 [RPC 응답 수신] ID: {Id}, Status: Success", command.MessageId);
            return response.Message;
        }
        catch (RequestTimeoutException)
        {
            logger.LogError("⚠️ [RPC 타임아웃] 명령 {Id}에 대한 응답이 {Timeout}초 내에 오지 않았습니다. (Type: {Type})", 
                command.MessageId, timeout.TotalSeconds, command.GetType().Name);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [RPC 통신 오류] {Id} 처리 중 예외 발생", command.MessageId);
            throw;
        }
    }
}
