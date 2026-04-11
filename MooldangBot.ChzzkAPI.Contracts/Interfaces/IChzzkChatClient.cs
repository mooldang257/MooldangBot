namespace MooldangBot.ChzzkAPI.Contracts.Interfaces;

/// <summary>
/// [?ㅼ떆由ъ뒪???⑤? - ?명꽣?섏씠??: ?щ윭 WebSocket ?ㅻ뱶瑜?愿由ы븯怨??몃?? ?뚰넻?섎뒗 理쒖긽??梨꾪똿 ?대씪?댁뼵???명꽣?섏씠?ㅼ엯?덈떎.
/// </summary>
public interface IChzzkChatClient
{
    /// <summary>
    /// [?꾩껜 ?곌껐]: ?뱀젙 梨꾨꼸???댁떆 湲곕컲?쇰줈 ?ㅻ뱶???좊떦?섍퀬 ?곌껐???쒖옉?⑸땲??
    /// </summary>
    Task ConnectAsync(string chzzkUid, string url, string accessToken);

    /// <summary>
    /// [?곌껐 醫낅즺]: ?뱀젙 梨꾨꼸??紐⑤뱺 ?뚯폆 ?곌껐???덉쟾?섍쾶 ?댁젣?⑸땲??
    /// </summary>
    Task DisconnectAsync(string chzzkUid);
}
