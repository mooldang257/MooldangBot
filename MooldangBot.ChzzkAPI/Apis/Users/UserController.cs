using Microsoft.AspNetCore.Mvc;
using MooldangBot.ChzzkAPI.Contracts.Interfaces;
using MooldangBot.ChzzkAPI.Contracts.Models.Chzzk.Users;

namespace MooldangBot.ChzzkAPI.Apis.Users;

/// <summary>
/// [?ㅼ떆由ъ뒪???ъ쁺]: 移섏?吏??ъ슜??蹂몄씤???뺣낫瑜?議고쉶?섎뒗 而⑦듃濡ㅻ윭?낅땲??
/// </summary>
[ApiController]
[Route("apis/chzzk/users")]
public class UserController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<UserController> _logger;

    public UserController(IChzzkApiClient apiClient, ILogger<UserController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [???뺣낫 議고쉶]: ?꾩옱 ?≪꽭???좏겙???뚯쑀???뺣낫瑜?媛?몄샃?덈떎.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe([FromHeader(Name = "Authorization")] string authHeader)
    {
        var accessToken = authHeader.Replace("Bearer ", "");
        var user = await _apiClient.GetUserMeAsync(accessToken);
        if (user == null) return Unauthorized("?ъ슜???뺣낫瑜?議고쉶?????놁뒿?덈떎.");

        return Ok(user);
    }
}
