using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Controllers.SongQueue;

/// <summary>
/// [v19.0] 스트리머 전용 노래책(SongBook) 관리 컨트롤러
/// 엑셀 일괄 처리 및 데이터 관리를 담당합니다.
/// </summary>
[ApiController]
[Route("api/songbook/{chzzkUid}")]
[Authorize(Policy = "chzzk-access")]
public class SongBookController(
    IAppDbContext db,
    ISongBookExcelService excelService,
    IIdentityCacheService identityCache) : ControllerBase
{
    private readonly IAppDbContext _db = db;
    private readonly ISongBookExcelService _excelService = excelService;
    private readonly IIdentityCacheService _identityCache = identityCache;

    /// <summary>
    /// [v19.0] 현재 노래책 데이터를 엑셀로 내보냅니다. (템플릿으로 활용 가능)
    /// </summary>
    [HttpGet("excel/export")]
    public async Task<IActionResult> ExportExcel(string chzzkUid)
    {
        var streamer = await GetCachedProfileAsync(chzzkUid);
        if (streamer == null) 
            return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

        var stream = await _excelService.ExportSongBookAsync(streamer.Id);
        var fileName = $"Mooldang_SongBook_{DateTime.Now:yyyyMMdd}.xlsx";
        
        // 브라우저에서 다운로드되도록 반환
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// [v19.0] 엑셀 파일을 업로드하여 노래책에 일괄 등록합니다.
    /// </summary>
    [HttpPost("excel/import")]
    public async Task<IActionResult> ImportExcel(string chzzkUid, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(Result<string>.Failure("엑셀 파일을 업로드해주세요."));

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(Result<string>.Failure("지원하지 않는 파일 형식입니다. .xlsx 파일만 업로드 가능합니다."));

        var streamer = await GetCachedProfileAsync(chzzkUid);
        if (streamer == null) 
            return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

        using var stream = file.OpenReadStream();
        var result = await _excelService.ImportSongBookAsync(streamer.Id, stream);

        return Ok(Result<SongBookImportResultDto>.Success(result));
    }

    private async Task<StreamerProfile?> GetCachedProfileAsync(string uid)
    {
        var profile = await _identityCache.GetStreamerProfileAsync(uid);
        if (profile != null) return profile;

        var target = uid.ToLower();
        return await _db.StreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == target || (p.Slug != null && p.Slug.ToLower() == target));
    }
}
