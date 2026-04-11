using System.Threading.Tasks;

namespace MooldangBot.ChzzkAPI.Contracts.Interfaces;

/// <summary>
/// [?ㅼ떆由ъ뒪??吏?섍?]: ?щ윭 媛쒖쓽 WebSocket ?ㅻ뱶瑜?珥앷큵 愿由ы븯湲??꾪븳 ?명꽣?섏씠?ㅼ엯?덈떎.
/// </summary>
public interface IShardedWebSocketManager
{
    /// <summary>
    /// ?뱀젙 梨꾨꼸(ChzzkUid)?????WebSocket ?곌껐???섑뻾?⑸땲??
    /// </summary>
    Task ConnectAsync(string chzzkUid, string url, string accessToken);

    /// <summary>
    /// ?뱀젙 梨꾨꼸??????곌껐???댁젣?⑸땲??
    /// </summary>
    Task DisconnectAsync(string chzzkUid);
}
