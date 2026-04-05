using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Presentation.Features.SongLibrary;

/// <summary>
/// [v13.0] 중앙 병기창(Master Song Library) 하이브리드 관제 컨트롤러
/// </summary>
[ApiController]
[Route("api/song-library")]
public class SongLibraryController : ControllerBase
{
    private readonly ISongLibraryService _libraryService;

    public SongLibraryController(ISongLibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    /// <summary>
    /// 노래 제목 또는 별칭(Alias)으로 마스터 라이브러리를 검색합니다. (유사도/초성 하이브리드 검색)
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<SongLibrarySearchResultDto>());

        var results = await _libraryService.SearchLibraryAsync(q);
        return Ok(results);
    }

    /// <summary>
    /// [v13.0] 현장에서 유입된 신규 곡 정보를 지능형 병기창(Staging)에 징집합니다.
    /// </summary>
    [HttpPost("capture")]
    [AllowAnonymous] // 스트리머 및 시청자 모두 유입 가능
    public async Task<IActionResult> Capture([FromBody] SongLibraryCaptureDto dto)
    {
        if (dto == null)
            return BadRequest();

        await _libraryService.CaptureStagingAsync(dto);
        return Ok(new { success = true });
    }
}
