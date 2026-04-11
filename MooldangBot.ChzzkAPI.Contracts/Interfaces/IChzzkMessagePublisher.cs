using System.Threading.Tasks;

namespace MooldangBot.ChzzkAPI.Contracts.Interfaces;

/// <summary>
/// [?ㅼ떆由ъ뒪???꾨졊]: 移섏?吏?寃뚯씠?몄썾?댁뿉??諛쒖깮???대깽?몃? 硫붿떆吏 釉뚮줈而?RabbitMQ)濡?諛쒗뻾?섍린 ?꾪븳 ?명꽣?섏씠?ㅼ엯?덈떎.
/// </summary>
public interface IChzzkMessagePublisher
{
    /// <summary>
    /// 移섏?吏?梨꾪똿 ?대깽?몃? 諛쒗뻾?⑸땲??
    /// </summary>
    Task PublishChatEventAsync(object chatEvent);

    /// <summary>
    /// 寃뚯씠?몄썾???곹깭 蹂寃??대깽?몃? 諛쒗뻾?⑸땲??
    /// </summary>
    Task PublishStatusEventAsync(string chzzkUid, string status);
}
