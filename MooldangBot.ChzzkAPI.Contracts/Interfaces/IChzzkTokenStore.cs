using System.Collections.Generic;
using System.Threading.Tasks;

namespace MooldangBot.ChzzkAPI.Contracts.Interfaces;

/// <summary>
/// [?ㅼ떆由ъ뒪???댁뇿]: 移섏?吏??몄쬆 ?좏겙(Cookie/Session)???덉쟾?섍쾶 蹂닿??섍퀬 愿由ы븯湲??꾪븳 ?명꽣?섏씠?ㅼ엯?덈떎.
/// </summary>
public interface IChzzkTokenStore
{
    /// <summary>
    /// ?뱀젙 梨꾨꼸???좏겙????ν빀?덈떎.
    /// </summary>
    Task SetTokenAsync(string chzzkUid, string sessionCookie, string authCookie);

    /// <summary>
    /// ?뱀젙 梨꾨꼸???좏겙??議고쉶?⑸땲??
    /// </summary>
    Task<(string SessionCookie, string AuthCookie)> GetTokenAsync(string chzzkUid);

    /// <summary>
    /// ?꾩옱 愿由?以묒씤 紐⑤뱺 梨꾨꼸???좏겙 ?뺣낫瑜?媛?몄샃?덈떎.
    /// </summary>
    Task<IDictionary<string, (string SessionCookie, string AuthCookie)>> GetAllTokensAsync();
}
