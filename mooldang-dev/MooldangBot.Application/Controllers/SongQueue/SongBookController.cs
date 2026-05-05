using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;
using MooldangBot.Application.Interfaces;
using MediatR;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using MooldangBot.Domain.Contracts.SongBook.Events;

namespace MooldangBot.Application.Controllers.SongQueue;

/// <summary>
/// [v19.0] 스트리머 전용 노래책(FuncSongBooks) 관리 컨트롤러
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
    private readonly IMediator _mediator;
    private readonly IVectorEmbeddingService _embeddingService;
    private readonly IVectorSearchRepository _vectorRepository;
    private readonly ILlmService _llmService;
    private readonly ILogger<SongBookController> _logger;

    public SongBookController(
        IAppDbContext db, 
        ICommonDbContext commonDb,
        ISongBookExcelService excelService,
        IIdentityCacheService identityCache,
        IEnumerable<ISongThumbnailService> thumbnailServices,
        IFileStorageService fileStorage,
        HttpClient httpClient,
        IMediator mediator,
        IVectorEmbeddingService embeddingService,
        IVectorSearchRepository vectorRepository,
        ILlmService llmService,
        ILogger<SongBookController> logger)
    {
        _db = db;
        _commonDb = commonDb;
        _excelService = excelService;
        _identityCache = identityCache;
        _thumbnailServices = thumbnailServices;
        _fileStorage = fileStorage;
        _httpClient = httpClient;
        _mediator = mediator;
        _embeddingService = embeddingService;
        _vectorRepository = vectorRepository;
        _llmService = llmService;
        _logger = logger;
    }

    /// <summary>
    /// [v26.5] 아티스트와 제목(또는 가사)으로 썸네일 후보를 검색합니다. (AI-First 단일 트랙)
    /// </summary>
    [HttpGet("thumbnail/search")]
    public async Task<IActionResult> SearchThumbnails(
        [FromQuery] string? query,
        [FromQuery(Name = "artist")] string? artist, 
        [FromQuery(Name = "title")] string? title)
    {
        var rawInput = $"{query} {artist} {title}".Trim();
        _logger.LogInformation("[SongBookThumbnail] 검색 요청 시작: {RawInput}", rawInput);

        if (string.IsNullOrWhiteSpace(rawInput))
            return Ok(Result<List<string>>.Success(new List<string>()));

        try
        {
            // [Track: AI 보정 및 정제]
            // 입력과 출력 포맷을 통일하여 AI가 구조적 변환(Normalization)에 집중하게 합니다.
            var inputData = $"ARTIST:{artist}|TITLE:{title}|RAW:{query}";
            
            var prompt = "You are an iTunes search expert. Normalize input into the most searchable 'Official Name'. \n" +
                         "RULES:\n" +
                         "1. J-Pop: Use Original Kanji/Kana. | Pop/K-Pop: Use Official English.\n" +
                         "2. Respond ONLY with 'ARTIST:name|TITLE:title'. NO CHATTING.\n\n" +
                         "EXAMPLES:\n" +
                         "Input: ARTIST:아도|TITLE:역광|RAW: -> Output: ARTIST:Ado|TITLE:逆光\n" +
                         "Input: ARTIST:뉴진스|TITLE:하우스윗|RAW: -> Output: ARTIST:NewJeans|TITLE:How Sweet";

            var aiResponse = await _llmService.GenerateResponseAsync(prompt, inputData);
            _logger.LogInformation("[SongBookThumbnail] AI 보정 결과 (Input: {InputData}, Output: {AIResponse})", inputData, aiResponse);

            string? refinedArtist = null;
            string? refinedTitle = null;

            if (!string.IsNullOrEmpty(aiResponse))
            {
                var parts = aiResponse.Split('|');
                foreach (var part in parts)
                {
                    if (part.Trim().StartsWith("ARTIST:")) refinedArtist = part.Replace("ARTIST:", "").Trim();
                    if (part.Trim().StartsWith("TITLE:")) refinedTitle = part.Replace("TITLE:", "").Trim();
                }
            }

            // AI 보정 결과가 없으면 원본값으로 시도 (Fallback)
            var finalArtist = refinedArtist ?? artist;
            var finalTitle = refinedTitle ?? title ?? query;

            // [iTunes 단일 검색 실행]
            var itunesService = _thumbnailServices.FirstOrDefault(s => s.GetType().Name.Contains("Itunes"));
            if (itunesService == null)
            {
                _logger.LogError("[SongBookThumbnail] iTunes 서비스를 찾을 수 없습니다.");
                return Ok(Result<List<string>>.Success(new List<string>()));
            }

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10.0));
            _logger.LogInformation("[SongBookThumbnail] iTunes 검색 실행 (Artist: {FinalArtist}, Title: {FinalTitle})", finalArtist ?? "None", finalTitle ?? "None");

            var candidates = await itunesService.SearchThumbnailsAsync(finalArtist, finalTitle, cts.Token);

            var finalUrls = candidates
                .Select(x => x.Url)
                .Distinct()
                .Take(30)
                .ToList();

            _logger.LogInformation("[SongBookThumbnail] 검색 최종 완료: {Count}건", finalUrls.Count);
            return Ok(Result<List<string>>.Success(finalUrls));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SongBookThumbnail] 검색 프로세스 중 치명적 오류 발생: {Message}", ex.Message);
            return Ok(Result<List<string>>.Success(new List<string>()));
        }
    }



    /// <summary>
    /// [v23.0] 노래책 목록을 조회합니다. (하이브리드 검색 지원)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSongs(string chzzkUid, [FromQuery] string? query, [FromQuery] string? category)
    {
        var streamer = await GetCachedProfileAsync(chzzkUid);
        if (streamer == null) 
            return NotFound(Result<string>.Failure("스트리머를 찾을 수 없습니다."));

        // [오시리스의 지혜]: 검색어가 있는 경우 하이브리드 검색 수행, 없으면 일반 필터링 조회
        if (!string.IsNullOrEmpty(query) && query.Length >= 2)
        {
            try
            {
                var queryVector = await _embeddingService.GetEmbeddingAsync(query);
                var searchResults = await _vectorRepository.SearchHybridForStreamerAsync<FuncSongBooks>(
                    chzzkUid, query, queryVector, 100);

                // 카테고리 필터링이 필요한 경우 메모리 내에서 수행 (데이터가 수천 건 이내이므로 안전)
                if (!string.IsNullOrEmpty(category) && category != "전체")
                {
                    searchResults = searchResults.Where(s => s.Category != null && s.Category.Contains(category));
                }

                var resultDtos = searchResults.Select(s => new SongBookDto
                {
                    Id = s.SongNo,
                    Title = s.Title,
                    Artist = s.Artist,
                    Category = s.Category,
                    Pitch = s.Pitch,
                    Proficiency = s.Proficiency,
                    LyricsUrl = s.LyricsUrl,
                    ReferenceUrl = s.ReferenceUrl,
                    ThumbnailUrl = s.ThumbnailUrl,
                    RequiredPoints = s.RequiredPoints,
                    UpdatedAt = s.UpdatedAt
                }).ToList();

                return Ok(Result<List<SongBookDto>>.Success(resultDtos));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HybridSearch] 검색 실패 (Fallback): {ex.Message}");
                // 검색 실패 시 일반 검색으로 Fallback
            }
        }

        // 일반 조회 및 키워드 필터링 (Fallback)
        var dbQuery = _db.TableFuncSongBooks
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
            .OrderBy(s => s.SongNo)
            .Select(s => new SongBookDto
            {
                Id = s.SongNo,
                Title = s.Title,
                Artist = s.Artist,
                Category = s.Category,
                Pitch = s.Pitch,
                Proficiency = s.Proficiency,
                LyricsUrl = s.LyricsUrl,
                ReferenceUrl = s.ReferenceUrl,
                ThumbnailUrl = s.ThumbnailUrl,
                RequiredPoints = s.RequiredPoints,
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

        // [v19.5] 중복 등록 방지 로직 (동일 스트리머 내 제목+가수 중복 불가)
        var existingSong = await _db.TableFuncSongBooks
            .FirstOrDefaultAsync(s => s.StreamerProfileId == profile.Id && 
                           s.Title == request.Title && 
                           s.Artist == request.Artist && 
                           !s.IsDeleted);

        if (existingSong != null)
        {
            // 중복 발견 시 409 Conflict 반환 (프론트엔드에서 모달을 띄우기 위함)
            return Conflict(Result<object>.Failure($"이미 존재하는 노래입니다.", existingSong.SongNo.ToString()));
        }

        // [v19.1] 다음 SongNo 계산 (1부터 시작)
        var maxSongNo = await _db.TableFuncSongBooks
            .IgnoreQueryFilters()
            .Where(s => s.StreamerProfileId == profile.Id)
            .Select(s => (int?)s.SongNo)
            .MaxAsync() ?? 0;

        var song = new FuncSongBooks
        {
            StreamerProfileId = profile.Id,
            SongNo = maxSongNo + 1,
            Title = request.Title,
            Artist = request.Artist,
            Category = request.Category,
            Pitch = request.Pitch,
            Proficiency = request.Proficiency,
            LyricsUrl = request.LyricsUrl,
            ReferenceUrl = request.ReferenceUrl,
            RequiredPoints = request.RequiredPoints
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
                    var existingThumb = await _commonDb.TableCommonThumbnail.FirstOrDefaultAsync(t => t.LocalPath == request.ThumbnailUrl);
                    if (existingThumb != null)
                    {
                        existingThumb.ReferenceCount++;
                        _commonDb.TableCommonThumbnail.Update(existingThumb);
                        await _commonDb.SaveChangesAsync();
                    }
                }
                else // 외부 URL인 경우 다운로드 및 해시 체크
                {
                    var imageBytes = await _httpClient.GetByteArrayAsync(request.ThumbnailUrl);
                    var hash = ComputeHash(imageBytes);

                    // 1. 이미 동일한 내용의 이미지가 있는지 해시로 체크
                    var sharedThumb = await _commonDb.TableCommonThumbnail.FirstOrDefaultAsync(t => t.FileHash == hash);

                    if (sharedThumb != null)
                    {
                        // 중복 발견: 기존 경로 재사용
                        song.ThumbnailUrl = sharedThumb.LocalPath;
                        sharedThumb.ReferenceCount++;
                        _commonDb.TableCommonThumbnail.Update(sharedThumb);
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
                        _commonDb.TableCommonThumbnail.Add(newThumb);
                        song.ThumbnailUrl = localPath;
                    }
                    await _commonDb.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FuncSongBooks] 썸네일 처리 실패: {ex.Message}");
                song.ThumbnailUrl = request.ThumbnailUrl;
            }
        }

        _db.TableFuncSongBooks.Add(song);
        await _db.SaveChangesAsync();

        // [오시리스의 영속]: 빅데이터 수집 - 벡터 검색용 전역 메타데이터 저장
        if (!string.IsNullOrWhiteSpace(song.ThumbnailUrl))
        {
            _ = Task.Run(async () => {
                try {
                    var artistNorm = (song.Artist ?? "Unknown").ToLower().Trim();
                    var titleNorm = song.Title.ToLower().Trim();
                    var query = $"{song.Artist} {song.Title}".Trim();
                    var vector = await _embeddingService.GetEmbeddingAsync(query);
                    await _vectorRepository.SaveMetadataAsync(
                        artistNorm, 
                        titleNorm, 
                        song.ThumbnailUrl, 
                        vector);
                } catch (Exception ex) {
                    Console.WriteLine($"[GlobalMetadata] 데이터 축적 실패: {ex.Message}");
                }
            });
        }

        // [오시리스의 예지]: 썸네일이 없는 경우 백그라운드 수집 이벤트 발행
        if (string.IsNullOrEmpty(song.ThumbnailUrl))
        {
            await _mediator.Publish(new SongMetadataFetchEvent(song.Artist ?? "", song.Title, song.Id));
        }

        return Ok(Result<object>.Success(new { Id = song.SongNo, Title = song.Title, LocalPath = song.ThumbnailUrl }));
    }

    /// <summary>
    /// [v19.5] 개별 곡 정보를 수정합니다.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSong(string chzzkUid, int id, [FromBody] SongBookDto request)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Unauthorized();

        var song = await _db.TableFuncSongBooks
            .FirstOrDefaultAsync(s => s.SongNo == id && s.StreamerProfileId == profile.Id && !s.IsDeleted);
        
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
        song.RequiredPoints = request.RequiredPoints;

        await _db.SaveChangesAsync();
        return Ok(Result<object>.Success(new { Id = song.SongNo, Title = song.Title }));
    }

    /// <summary>
    /// [v19.5] 개별 곡을 삭제합니다. (소프트 삭제)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSong(string chzzkUid, int id)
    {
        var profile = await GetCachedProfileAsync(chzzkUid);
        if (profile == null) return Unauthorized();

        var song = await _db.TableFuncSongBooks
            .FirstOrDefaultAsync(s => s.SongNo == id && s.StreamerProfileId == profile.Id && !s.IsDeleted);
        
        if (song == null)
            return NotFound(Result<string>.Failure("곡을 찾을 수 없습니다."));

        song.IsDeleted = true;
        await _db.SaveChangesAsync();

        return Ok(Result<object>.Success(new { Id = song.SongNo }));
    }

    private string ComputeHash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private async Task<CoreStreamerProfiles?> GetCachedProfileAsync(string uid)
    {
        var profile = await _identityCache.GetStreamerProfileAsync(uid);
        if (profile != null) return profile;

        var target = uid.ToLower();
        return await _db.TableCoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid.ToLower() == target || (p.Slug != null && p.Slug.ToLower() == target));
    }
}
