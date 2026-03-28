using System.Threading.Tasks;

namespace MooldangBot.Application.Interfaces
{
    /// <summary>
    /// [v1.8] 챗봇 답변 내의 {동적변수}를 실시간 DB 쿼리로 치환하는 엔진
    /// </summary>
    public interface IDynamicQueryEngine
    {
        /// <summary>
        /// 메시지 내의 {키워드}를 찾아 DB 쿼리 결과값으로 치환합니다.
        /// </summary>
        /// <param name="message">원본 메시지 (예: "현재 포인트는 {포인트}입니다.")</param>
        /// <param name="streamerChzzkUid">현재 채널의 스트리머 ID</param>
        /// <param name="viewerUid">메시지를 보낸 시청자의 ID</param>
        /// <returns>치환된 메시지</returns>
        Task<string> ProcessMessageAsync(string message, string streamerChzzkUid, string viewerUid);
    }
}
