using MassTransit;
using Microsoft.Extensions.Logging;
using MooldangBot.Contracts.Common.Messages;
using System.Threading.Tasks;

namespace MooldangBot.Application.Consumers;

/// <summary>
/// [오시리스의 수신함]: RabbitMQ를 통해 전파된 통합 이벤트를 수신하여 사후 처리를 담당하는 컨슈머입니다.
/// 메인 엔진의 부하를 줄이기 위해 로그 기록, 통계 분석, 외부 시스템 알림 등을 이곳에서 처리합니다.
/// </summary>
public class CommandLogIntegrationConsumer(ILogger<CommandLogIntegrationConsumer> logger) 
    : IConsumer<CommandExecutedIntegrationEvent>
{
    /// <summary>
    /// 외부로 사출된 통합 이벤트를 수신하여 소화합니다.
    /// </summary>
    public async Task Consume(ConsumeContext<CommandExecutedIntegrationEvent> context)
    {
        var message = context.Message;

        // [v6.0] 지휘관 지시: 함선 외부 워커의 행동
        // 메인 엔진은 "채팅 응답"에 집중하고, 이 워커는 "데이터 분석 및 영구 기록"에 집중합니다.
        
        logger.LogInformation("📥 [Integration Consumer] 외부 수신함 소식 도착! (CorrelationId: {Id})", message.CorrelationId);
        logger.LogInformation("📓 [분석 레포트] {Viewer}님이 {Keyword} 명령어를 구사함. (Raw: {Raw})", 
            message.ViewerNickname, message.Keyword, message.RawMessage);

        // 향후 이곳에 ElasticSearch 저장, 빅데이터 분석, 혹은 디스코드 웹훅 알림 등을 이식할 수 있습니다.
        await Task.CompletedTask;
    }
}
