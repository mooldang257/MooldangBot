using Microsoft.AspNetCore.Mvc;

namespace MooldangBot.ChzzkAPI.Apis.Base;

/// <summary>
/// [?뚮줈?ㅼ쓽 湲곕컲]: ?곕━ ?쒖뒪???대? ?듭떊 洹쒖빟???곕Ⅴ??API 湲곕컲 컨트롤러?낅땲??
/// 紐⑤뱺 ?묐떟? 媛怨??놁씠 ?쒖닔 ?곗씠??HTTP 200) ?먮뒗 ?대? ?먮윭 洹쒓꺽?쇰줈 諛섑솚?⑸땲??
/// </summary>
[ApiController]
[Route("api/v1/chzzk/[controller]")]
[Produces("application/json")]
public abstract class ChzzkBaseController(ILogger logger) : ControllerBase
{
    protected readonly ILogger _logger = logger;

    /// <summary>
    /// [?대? 洹쒖빟 ?묐떟]: ?몃? 洹쒓꺽(?섑띁) ?놁씠 ?쒖닔 ?곗씠?곕? 諛섑솚?⑸땲??
    /// ?먮윭 泥섎━??ChzzkExceptionFilter?먯꽌 ?쇨큵 ?섑뻾?⑸땲??
    /// </summary>
    protected IActionResult Success<T>(T content)
    {
        return Ok(content);
    }
}
