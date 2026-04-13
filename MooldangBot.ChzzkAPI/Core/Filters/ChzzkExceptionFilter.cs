using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Chzzk.Shared;

namespace MooldangBot.ChzzkAPI.Core.Filters;

/// <summary>
/// [?ㅼ떆由ъ뒪??以묒옱]: 寃뚯씠?몄썾???대??먯꽌 諛쒖깮?섎뒗 ?덉쇅瑜?移섏?吏??쒖? ?묐떟 洹쒓꺽?쇰줈 蹂?섑빀?덈떎.
/// ?꾨찓???덉씠???섏〈?깆쓣 ?꾩쟾???쒓굅?섏??듬땲??
/// </summary>
public class ChzzkExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ChzzkExceptionFilter> _logger;

    public ChzzkExceptionFilter(ILogger<ChzzkExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "??[ChzzkAPI] ?덉쇅 諛쒖깮: {Message}", context.Exception.Message);

        int statusCode = 500;
        string message = "?대? ?쒕쾭 ?ㅻ쪟媛 諛쒖깮?덉뒿?덈떎.";

        if (context.Exception is ArgumentException or BadHttpRequestException)
        {
            statusCode = 400;
            message = "遺?곸젅???붿껌?낅땲??";
        }
        else if (context.Exception is UnauthorizedAccessException)
        {
            statusCode = 401;
            message = "?몄쬆???ㅽ뙣?섏??듬땲??";
        }

        // [v3.3] 移섏?吏?怨듯넻 ?묐떟 遊됲닾(Shared) 洹쒓꺽??留욎떠 ?먮윭 諛섑솚
        var errorResponse = new ChzzkApiResponse<string>
        {
            Code = statusCode,
            Message = message,
            Content = null
        };

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }
}
