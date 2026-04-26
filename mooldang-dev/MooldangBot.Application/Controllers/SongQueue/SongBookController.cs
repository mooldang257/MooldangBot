using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Contracts.SongBook;

namespace MooldangBot.Application.Controllers.SongQueue;

/// <summary>
/// [v19.0] 스트리머 전용 노래책(SongBook) 관리 컨트롤러
/// 엑셀 일괄 처리 및 데이터 관리를 담당합니다.
/// </summary>
[ApiController]
[Route("api/songbook/{chzzkUid}")]
[Authorize(Policy = "chzzk-access")]
public class SongBookController : ControllerBase
{
    private readonly IAppDbContext _db; 
    private readonly ICommonDbContext _commonDb; 
    private readonly ISongBookExcelService _excelService;
    private readonly IIdentityCacheService _identityCache;
    private readonly IEnumerable<ISongThumbnailService> _thumbnailServices;
    private readonly IFileStorageService _fileStorage;
    private readonly HttpClient _httpClient;

    public SongBookController(
        IAppDbContext db, 
        ICommonDbContext commonDb,
        ISongBookExcelService excelService,
        IIdentityCacheService identityCache,
        IEnumerable<ISongThumbnailService> thumbnailServices,
        IFileStorageService fileStorage,
        HttpClient httpClient)
    {
        _db = db;
        _commonDb = commonDb;
        _excelService = excelService;
        _identityCache = identityCache;
        _thumbnailServices = thumbnailServices;
        _fileStorage = fileStorage;
        _httpClient = httpClient;
    }

    /// <summary>
    /// [v19.5] 아티스트와 제목으로 썸네일(앨범 아트) 후보를 검색합니다.
    /// 로컬 도서관(1순위) + iTunes + YouTube 검색 결과를 통합하여 제공합니다.
    /// </summary>
    [HttpGet("thumbnail/search")]
    public async Task<IActionResult> SearchThumbnails(string? artist, string? title)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
            return BadRequest(Result<System.Collections.Generic.List<string>>.Failure("곡 제목이나 가수 이름을 입력해주세요."));

        var allResults = new List<string>();

        // [오시리스의 도서관]: 1순위로 우리 서버 공용 저장소 확인
        var localCandidates = await _commonDb.Thumbnails
            .Where(t => t.Title == title || t.Artist == artist)
            .OrderByDescending(t => t.ReferenceCount)
            .Select(t => t.LocalPath)
            .Take(10)
            .ToListAsync();

        if (localCandidates.Any()) allResults.AddRange(localCandidates);
        
        // 2순위: 외부 검색 엔진(iTunes, YouTube 등)
        var searchTasks = _thumbnailServices.Select(s => s.SearchThumbnailsAsync(artist, title));
        var resultsArray = await Task.WhenAll(searchTasks);

        foreach (var results in resultsArray)
        {
            if (results != null) allResults.AddRange(results);
        }

        // 중복 제거 및 최대 40개로 확장
        var finalResults = allResults.Distinct().Take(40).ToList();

        return Ok(Result<System.Collections.Generic.List<string>>.Success(finalResults));
    }

    /// <summary>
    /// [v19.1] 노래책 목록을 조회합니다.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSongs(string chzzkUid, [FromQuery] string? query, [FromQuery] string? category)
    {
        var streamer = await GetCachedProfileAsync(chzzkUid);
        if (streamer == null) 
            return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

        var dbQuery = _db.FuncSongBooks
            .AsNoTracking()
            .Where(s => s.StreamerProfileId == streamer.Id && !s.IsDeleted);

        if (!string.IsNullOrEmpty(query))
        {
            var search = query.ToLower();
            dbQuery = dbQuery.Where(s => s.Title.ToLower().Contains(search) || (s.Artist != null && s.Artist.ToLower().Contains(search)));
        }

        if (!string.IsNullOrEmpty(category) && category != "전체")
        {
            dbQuery = dbQuery.Where(s => s.Category != null && s.Category.Contains(category));
        }

        var songs = await dbQuery
            .OrderByDescending(s => s.Id)
            .Select(s => new SongBookDto
            {
                Id = s.Id,
                Title = s.Title,
                Artist = s.Artist,
                Category = s.Category,
                Pitch = s.Pitch,
                Proficiency = s.Proficiency,
                LyricsUrl = s.LyricsUrl,
                ReferenceUrl = s.ReferenceUrl,
                ThumbnailUrl = s.ThumbnailUrl,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return Ok(Result<List<SongBookDto>>.Success(songs));
    }

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

    /// <summary>
    /// [v19.5] 개별 곡을 수동으로 등록합니다. 
    /// 썸네일은 SHA-256 해시를 통해 공용 도서관에 중복 없이 저장됩니다.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddSong(string chzzkUid, [FromBody] SongBookDto request)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Unauthorized();

        var song = new SongBook
        {
            StreamerProfileId = profile.Id,
            Title = request.Title,
            Artist = request.Artist,
            Category = request.Category,
            Pitch = request.Pitch,
            Proficiency = request.Proficiency,
            LyricsUrl = request.LyricsUrl,
            ReferenceUrl = request.ReferenceUrl
        };

        // [v19.5] 지능형 썸네일 처리 (공용 도서관 연동)
        if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl))
        {
            try
            {
                // 이미 우리 서버 내의 경로(/uploads/...)인 경우 (도서관에서 선택한 경우)
                if (request.ThumbnailUrl.StartsWith("/") || !request.ThumbnailUrl.StartsWith("http"))
                {
                    song.ThumbnailUrl = request.ThumbnailUrl;
                    // 해당 이미지의 레퍼런스 카운트 증가
                    var existingThumb = await _commonDb.Thumbnails.FirstOrDefaultAsync(t => t.LocalPath == request.ThumbnailUrl);
                    if (existingThumb != null)
                    {
                        existingThumb.ReferenceCount++;
                        _commonDb.Thumbnails.Update(existingThumb);
                        await _commonDb.SaveChangesAsync();
                    }
                }
                else // 외부 URL인 경우 다운로드 및 해시 체크
                {
                    var imageBytes = await _httpClient.GetByteArrayAsync(request.ThumbnailUrl);
                    var hash = ComputeHash(imageBytes);

                    // 1. 이미 동일한 내용의 이미지가 있는지 해시로 체크
                    var sharedThumb = await _commonDb.Thumbnails.FirstOrDefaultAsync(t => t.FileHash == hash);

                    if (sharedThumb != null)
                    {
                        // 중복 발견: 기존 경로 재사용
                        song.ThumbnailUrl = sharedThumb.LocalPath;
                        sharedThumb.ReferenceCount++;
                        _commonDb.Thumbnails.Update(sharedThumb);
                    }
                    else
                    {
                        // 신규 이미지: 저장 및 도서관 등록
                        var extension = ".webp"; // WebP 권장 또는 원본 추적
                        var fileName = $"{hash}{extension}";
                        var localPath = await _fileStorage.SaveFileAsync(imageBytes, "library/songs", fileName);

                        var newThumb = new CommonThumbnail
                        {
                            FileHash = hash,
                            Artist = request.Artist,
                            Title = request.Title,
                            LocalPath = localPath,
                            SourceUrl = request.ThumbnailUrl,
                            ReferenceCount = 1
                        };
                        _commonDb.Thumbnails.Add(newThumb);
                        song.ThumbnailUrl = localPath;
                    }
                    await _commonDb.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SongBook] 썸네일 처리 실패: {ex.Message}");
                song.ThumbnailUrl = request.ThumbnailUrl;
            }
        }

        _db.FuncSongBooks.Add(song);
        await _db.SaveChangesAsync();

        return Ok(Result<object>.Success(new { id = song.Id, title = song.Title, localPath = song.ThumbnailUrl }));
    }

    /// <summary>
    /// [v19.5] 개별 곡 정보를 수정합니다.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSong(string chzzkUid, int id, [FromBody] SongBookDto request)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Unauthorized();

        var song = await _db.FuncSongBooks
            .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfileId == profile.Id && !s.IsDeleted);
        
        if (song == null)
            return NotFound(Result<string>.Failure("곡을 찾을 수 없습니다."));

        if (!string.IsNullOrWhiteSpace(request.Title)) song.Title = request.Title;
        if (request.Artist != null) song.Artist = request.Artist;
        if (request.Category != null) song.Category = request.Category;
        if (request.Pitch != null) song.Pitch = request.Pitch;
        if (request.Proficiency != null) song.Proficiency = request.Proficiency;
        if (request.LyricsUrl != null) song.LyricsUrl = request.LyricsUrl;
        if (request.ReferenceUrl != null) song.ReferenceUrl = request.ReferenceUrl;
        if (request.ThumbnailUrl != null) song.ThumbnailUrl = request.ThumbnailUrl;

        await _db.SaveChangesAsync();
        return Ok(Result<object>.Success(new { id = song.Id, title = song.Title }));
    }

    /// <summary>
    /// [v19.5] 개별 곡을 삭제합니다. (소프트 삭제)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSong(string chzzkUid, int id)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Unauthorized();

        var song = await _db.FuncSongBooks
            .FirstOrDefaultAsync(s => s.Id == id && s.StreamerProfileId == profile.Id && !s.IsDeleted);
        
        if (song == null)
            return NotFound(Result<string>.Failure("곡을 찾을 수 없습니다."));

        song.IsDeleted = true;
        await _db.SaveChangesAsync();

        return Ok(Result<object>.Success(new { id = song.Id }));
    }

    private string ComputeHash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private async Task<StreamerProfile?> GetCachedProfileAsync(string uid)
    {
        var profile = await _identityCache.GetStreamerProfileAsync(uid);
        if (profile != null) return profile;

        var target = uid.ToLower();
        return await _db.CoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == target || (p.Slug != null && p.Slug.ToLower() == target));
    }
}
