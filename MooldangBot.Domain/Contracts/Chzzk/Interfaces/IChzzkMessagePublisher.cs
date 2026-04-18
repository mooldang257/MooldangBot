using System.Threading.Tasks;

namespace MooldangBot.Domain.Contracts.Chzzk.Interfaces;

/// <summary>
/// [오시리스의 명령]: 치지직 게이트웨이에서 발생한 이벤트를 메시지 브로커(RabbitMQ)로 발행하기 위한 인터페이스입니다.
/// </summary>
public interface IChzzkMessagePublisher
{
    /// <summary>
    /// [v3.7] 현대화된 다형성 치지직 이벤트를 발행합니다.
    /// </summary>
    /// <param name="envelope">다형성 페이로드가 포함된 이벤트 봉투</param>
    Task PublishEventAsync(MooldangBot.Domain.Contracts.Chzzk.Models.Events.ChzzkEventEnvelope envelope);

    /// <summary>
    /// 게이트웨이 상태 변경 이벤트를 발행합니다.
    /// </summary>
    Task PublishStatusEventAsync(string chzzkUid, string status);
}
