using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common.Models;

namespace MooldangBot.Application.Controllers.SongLibrary;

/// <summary>
/// [v13.0] 중앙 병기창(Master Song Library) 하이브리드 관제 컨트롤러
/// </summary>
[ApiController]
[Route("api/song-library")]
// [v10.1] Primary Constructor 적용
public class SongLibraryController(ISongLibraryService libraryService) : ControllerBase
{
    /// <summary>
    /// 노래 제목 또는 별칭(Alias)으로 마스터 라이브러리를 검색합니다. (유사도/초성 하이브리드 검색)
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Result<List<SongLibrarySearchResultDto>>.Success(new List<SongLibrarySearchResultDto>()));

        var results = await libraryService.SearchLibraryAsync(q);
        return Ok(Result<List<SongLibrarySearchResultDto>>.Success(results));
    }

    /// <summary>
    /// [v13.0] 현장에서 유입된 신규 곡 정보를 지능형 병기창(Staging)에 징집합니다.
    /// </summary>
    [HttpPost("capture")]
    [AllowAnonymous] 
    public async Task<IActionResult> Capture([FromBody] SongLibraryCaptureDto dto)
    {
        if (dto == null)
            return BadRequest(Result<string>.Failure("잘못된 요청 데이터입니다."));

        await libraryService.CaptureStagingAsync(dto);
        return Ok(Result<object>.Success(new { success = true }));
    }
}
