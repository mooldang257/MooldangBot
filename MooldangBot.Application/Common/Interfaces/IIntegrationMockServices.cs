using System.Threading.Tasks;

namespace MooldangBot.Application.Common.Interfaces;



/// <summary>
/// [실전 발화]: 생성된 메시지를 실제 치지직 채팅창으로 전송하는 인터페이스입니다.
/// </summary>
public interface IChzzkChatService
{
    /// <summary>
    /// [거울의 울림]: 특정 스트리머의 채팅창에 메시지를 전송합니다. (v1.9 동적 엔진용 viewerUid 추가)
    /// </summary>
    Task SendMessageAsync(string chzzkUid, string message, string viewerUid, System.Threading.CancellationToken ct = default);
}
