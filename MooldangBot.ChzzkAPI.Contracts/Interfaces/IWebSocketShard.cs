using System;
using System.Threading.Tasks;

namespace MooldangBot.ChzzkAPI.Contracts.Interfaces;

/// <summary>
/// [?ㅼ떆由ъ뒪???명룷]: 媛쒕퀎 移섏?吏?梨꾨꼸怨쇱쓽 WebSocket ?곌껐 諛??듭떊???대떦?섎뒗 ?명꽣?섏씠?ㅼ엯?덈떎.
/// </summary>
public interface IWebSocketShard : IDisposable
{
    /// <summary>
    /// ?ㅻ뱶??怨좎쑀 ?몃뜳?ㅼ엯?덈떎.
    /// </summary>
    int ShardId { get; }

    /// <summary>
    /// ?뱀젙 梨꾨꼸??????ㅼ떆媛??곌껐???섑뻾?⑸땲??
    /// </summary>
    Task ConnectAsync(string chzzkUid, string url, string accessToken);
}
