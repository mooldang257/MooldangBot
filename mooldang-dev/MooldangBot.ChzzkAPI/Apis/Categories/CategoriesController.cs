using Microsoft.AspNetCore.Mvc;
using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Categories;
using MooldangBot.Domain.Contracts.Chzzk.Models.Chzzk.Shared;

namespace MooldangBot.ChzzkAPI.Apis.Categories;

/// <summary>
/// [오시리스??吏?앷퀬]: 치지직諛⑹넚 移댄뀒怨좊━ 寃?됱쓣 담당하는 컨트롤러?낅땲??
/// </summary>
[ApiController]
[Route("apis/chzzk/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IChzzkApiClient _apiClient;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IChzzkApiClient apiClient, ILogger<CategoriesController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// [移댄뀒怨좊━ 寃??: 寃뚯엫 紐낆묶 ?깆쑝濡?치지직移댄뀒怨좊━瑜?寃?됲빀?덈떎.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("寃?됱뼱瑜??낅젰?댁＜?몄슂.");

        var result = await _apiClient.SearchCategoryAsync(query);
        return Ok(result);
    }
}
