using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 전령 RPC 실구현체]: MassTransit의 IRequestClient를 사용하여 게이트웨이와 통신합니다.
/// (v4.1): dynamic 바인딩 오류(RuntimeBinderException) 해결을 위해 명시적 리플렉션 호출 방식으로 전환되었습니다.
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
            var actualType = command.GetType();
            logger.LogDebug("🚀 [RPC 명령 송신 준비] 유형: {Type}, ID: {Id}", actualType.Name, command.MessageId);

            // 2. IRequestClient<T> 타입 생성 및 DI 조회
            // [시니어 팁]: MassTransit은 제네릭 타입 파라미터를 기준으로 URN을 생성하므로 이 과정이 필수적입니다.
            var requestClientType = typeof(IRequestClient<>).MakeGenericType(actualType);
            var client = serviceProvider.GetRequiredService(requestClientType);

            // 3. GetResponse<TResponse> 메서드 찾기 (다중 오버로딩 중 적합한 시그니처 선택)
            var methods = requestClientType.GetMethods()
                .Where(m => m.Name == "GetResponse" && m.IsGenericMethodDefinition);
            
            var targetMethod = methods.FirstOrDefault(m => {
                var parameters = m.GetParameters();
                // (T message, CancellationToken cancellationToken, RequestTimeout timeout) 시그니처 매칭
                return parameters.Length >= 2 && 
                       parameters[0].ParameterType == actualType && 
                       parameters[1].ParameterType == typeof(CancellationToken);
            });

            if (targetMethod == null)
            {
                throw new InvalidOperationException($"[{actualType.Name}]에 대한 GetResponse 메서드를 찾을 수 없습니다.");
            }

            // 4. 제네릭 타입 파라미터(TResponse) 주입
            var genericMethod = targetMethod.MakeGenericMethod(typeof(TResponse));

            // 5. 명시적 리플렉션 호출
            // RequestTimeout 기본값을 처리하기 위해 파라미터 배열을 구성합니다.
            var invokeArgs = new object[] { command, default(CancellationToken), default(RequestTimeout) };
            var task = (Task)genericMethod.Invoke(client, invokeArgs)!;

            // 6. 비동기 Task 안전하게 대기
            await task.ConfigureAwait(false);

            // 7. Task<Response<TResponse>> 에서 Result(Response<TResponse>) 추출
            var responseProperty = task.GetType().GetProperty("Result");
            var responseObj = responseProperty!.GetValue(task);

            // 8. Response<TResponse> 에서 Message(TResponse) 추출
            var messageProperty = responseObj!.GetType().GetProperty("Message");
            var resultMessage = (TResponse)messageProperty!.GetValue(responseObj)!;

            logger.LogDebug("📥 [RPC 응답 수신 성공] ID: {Id}, Type: {Type}", command.MessageId, actualType.Name);
            return resultMessage;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is RequestTimeoutException)
        {
            logger.LogError("⚠️ [RPC 타임아웃] 게이트웨이가 명령 {Id}({Type})에 응답하지 않습니다.", 
                command.MessageId, command.GetType().Name);
            throw ex.InnerException;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [RPC 통신 오류] 명령 {Id}({Type}) 처리 중 런타임 예외 발생", 
                command.MessageId, command.GetType().Name);
            throw;
        }
    }
}
