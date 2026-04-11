using System.Collections.Concurrent;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;

namespace MooldangBot.ChzzkAPI.Services;

/// <summary>
/// [?ㅼ떆由ъ뒪???댁뇿 蹂닿???: 移섏?吏??몄쬆 ?좏겙?ㅼ쓣 硫붾え由ъ긽?먯꽌 愿由ы븯???ㅻ젅???몄씠????μ냼?낅땲??
/// </summary>
public class InMemoryChzzkTokenStore : IChzzkTokenStore
{
    private readonly ConcurrentDictionary<string, (string SessionCookie, string AuthCookie)> _tokens = new();

    public Task SetTokenAsync(string chzzkUid, string sessionCookie, string authCookie)
    {
        _tokens[chzzkUid] = (sessionCookie, authCookie);
        return Task.CompletedTask;
    }

    public Task<(string SessionCookie, string AuthCookie)> GetTokenAsync(string chzzkUid)
    {
        if (_tokens.TryGetValue(chzzkUid, out var token))
        {
            return Task.FromResult(token);
        }
        return Task.FromResult<(string, string)>((string.Empty, string.Empty));
    }

    public Task<IDictionary<string, (string SessionCookie, string AuthCookie)>> GetAllTokensAsync()
    {
        IDictionary<string, (string, string)> result = _tokens.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return Task.FromResult(result);
    }
}
