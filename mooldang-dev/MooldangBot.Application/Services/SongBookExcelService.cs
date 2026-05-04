using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Application.Common.Utils;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.DTOs;
using MediatR;
using MooldangBot.Domain.Contracts.SongBook.Events;

namespace MooldangBot.Application.Services;

/// <summary>
/// [v19.0] ClosedXML 기반 노래책 일괄 처리 구현체
/// </summary>
public class SongBookExcelService(
    IAppDbContext dbContext,
    ISongLibraryService libraryService,
    IMediator mediator) : ISongBookExcelService
{
    private readonly IAppDbContext _db = dbContext;
    private readonly ISongLibraryService _libraryService = libraryService;
    private readonly IMediator _mediator = mediator;

    // [컬럼 이름 정의]: 내보내기/가져오기 시 공유됨
    private const string ColId = "번호";
    private const string ColTitle = "곡 제목";
    private const string ColArtist = "아티스트/가수";
    private const string ColCategory = "카테고리";
    private const string ColPitch = "키(Pitch)";
    private const string ColProficiency = "숙련도";
    private const string ColYoutube = "유튜브 링크";
    private const string ColLyrics = "가사 링크";
    private const string ColThumbnail = "섬네일 링크";
    private const string ColPoints = "필요포인트";
    private const string ColAlias = "검색태그";

    private static readonly string[] ExcelHeaders = 
    [ 
        ColId, ColTitle, ColArtist, ColCategory, ColPitch, 
        ColProficiency, ColYoutube, ColLyrics, ColThumbnail, 
        ColPoints, ColAlias 
    ];

    public async Task<Stream> ExportSongBookAsync(int streamerProfileId)
    {
        // 1. 데이터 조회 (활성 곡 & 삭제된 곡 분리)
        var allSongs = await _db.TableFuncSongBooks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.StreamerProfileId == streamerProfileId)
            .OrderBy(s => s.SongNo)
            .ToListAsync();

        var activeSongs = allSongs.Where(s => !s.IsDeleted).ToList();
        var deletedSongs = allSongs.Where(s => s.IsDeleted).ToList();

        // 2. ClosedXML을 사용하여 엑셀 생성
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        
        // --- 시트 1: 활성 노래책 ---
        var sheet1 = workbook.Worksheets.Add("노래책");
        for (int i = 0; i < ExcelHeaders.Length; i++) sheet1.Cell(1, i + 1).Value = ExcelHeaders[i];

        for (int i = 0; i < activeSongs.Count; i++)
        {
            var song = activeSongs[i];
            int row = i + 2;
            sheet1.Cell(row, 1).Value = song.SongNo;
            sheet1.Cell(row, 2).Value = song.Title;
            sheet1.Cell(row, 3).Value = song.Artist;
            sheet1.Cell(row, 4).Value = song.Category;
            sheet1.Cell(row, 5).Value = song.Pitch ?? "원키";
            sheet1.Cell(row, 6).Value = song.Proficiency ?? "완창";
            sheet1.Cell(row, 7).Value = song.ReferenceUrl;
            sheet1.Cell(row, 8).Value = song.LyricsUrl;
            sheet1.Cell(row, 9).Value = song.ThumbnailUrl;
            sheet1.Cell(row, 10).Value = song.RequiredPoints;
            sheet1.Cell(row, 11).Value = song.Alias;
        }

        // --- 시트 2: 삭제된 곡 목록 (조회용) ---
        var sheet2 = workbook.Worksheets.Add("삭제된 곡 목록");
        for (int i = 0; i < ExcelHeaders.Length; i++) sheet2.Cell(1, i + 1).Value = ExcelHeaders[i];

        for (int i = 0; i < deletedSongs.Count; i++)
        {
            var song = deletedSongs[i];
            int row = i + 2;
            sheet2.Cell(row, 1).Value = song.SongNo;
            sheet2.Cell(row, 2).Value = song.Title;
            sheet2.Cell(row, 3).Value = song.Artist;
            sheet2.Cell(row, 4).Value = song.Category;
            sheet2.Cell(row, 5).Value = song.Pitch ?? "원키";
            sheet2.Cell(row, 6).Value = song.Proficiency ?? "완창";
            sheet2.Cell(row, 7).Value = song.ReferenceUrl;
            sheet2.Cell(row, 8).Value = song.LyricsUrl;
            sheet2.Cell(row, 9).Value = song.ThumbnailUrl;
            sheet2.Cell(row, 10).Value = song.RequiredPoints;
            sheet2.Cell(row, 11).Value = song.Alias;
        }

        // 공통 스타일 적용 (시트 1, 2 모두)
        foreach (var ws in workbook.Worksheets)
        {
            var headerRange = ws.Range(1, 1, 1, ExcelHeaders.Length);
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#007BFF");
            headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            ws.Columns(1, ExcelHeaders.Length).Width = 20;
            ws.Column(1).Width = 10; // 번호
            ws.Column(2).Width = 35; // 곡 제목
            ws.Column(3).Width = 25; // 아티스트
            ws.Column(7).Width = 50; // 유튜브
            ws.Column(8).Width = 50; // 가사
            ws.Column(9).Width = 50; // 썸네일
            ws.Column(11).Width = 30; // 검색태그

            int dataRows = ws.RowsUsed().Count();
            if (dataRows > 0) ws.Range(1, 1, dataRows, ExcelHeaders.Length).SetAutoFilter();
        }

        var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }

    public async Task<SongBookImportResultDto> ImportSongBookAsync(int streamerProfileId, Stream excelStream)
    {
        var result = new SongBookImportResultDto { TotalCount = 0, SuccessCount = 0 };
        
        try
        {
            // 1. 엑셀 데이터 읽기 (ClosedXML 사용)
            using var workbook = new ClosedXML.Excel.XLWorkbook(excelStream);
            var worksheet = workbook.Worksheet(1);

            // [헤더 위치 동적 매핑]: 컬럼 순서가 바뀌거나 일부가 없어도 이름만 맞으면 동작함
            var firstRow = worksheet.Row(1);
            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in firstRow.CellsUsed())
            {
                var headerName = cell.GetValue<string>().Trim();
                if (!string.IsNullOrEmpty(headerName)) colMap[headerName] = cell.Address.ColumnNumber;
            }

            // 인덱스 확보 헬퍼
            int GetCol(string name) => colMap.TryGetValue(name, out int idx) ? idx : -1;

            int colIdIdx = GetCol(ColId);
            int colTitleIdx = GetCol(ColTitle);
            int colArtistIdx = GetCol(ColArtist);
            int colCategoryIdx = GetCol(ColCategory);
            int colPitchIdx = GetCol(ColPitch);
            int colProficiencyIdx = GetCol(ColProficiency);
            int colYoutubeIdx = GetCol(ColYoutube);
            int colLyricsIdx = GetCol(ColLyrics);
            int colThumbnailIdx = GetCol(ColThumbnail);
            int colPointsIdx = GetCol(ColPoints);
            int colAliasIdx = GetCol(ColAlias);

            // 필수 컬럼 존재 여부 확인
            if (colTitleIdx == -1)
            {
                result.Errors.Add($"'{ColTitle}' 컬럼이 엑셀에 없습니다. 헤더 이름을 확인해주세요.");
                return result;
            }

            var rows = worksheet.RowsUsed().Skip(1); // 헤더 제외

            // 2. 현재 스트리머의 곡 목록 미리 로드
            var existingSongs = await _db.TableFuncSongBooks
                .IgnoreQueryFilters()
                .Where(s => s.StreamerProfileId == streamerProfileId)
                .ToListAsync();

            var maxSongNo = existingSongs.Any() ? existingSongs.Max(s => s.SongNo) : 0;
            var processedSongNos = new HashSet<int>();

            foreach (var row in rows)
            {
                var cellTitle = row.Cell(colTitleIdx).GetValue<string>();
                if (string.IsNullOrWhiteSpace(cellTitle)) continue;

                try
                {
                    // 3. 라이브러리 ID 확보 (지능형 매칭)
                    var cellArtist = colArtistIdx != -1 ? row.Cell(colArtistIdx).GetValue<string>() : "Unknown";
                    var cellYoutube = colYoutubeIdx != -1 ? row.Cell(colYoutubeIdx).GetValue<string>() : string.Empty;
                    var cellLyrics = colLyricsIdx != -1 ? row.Cell(colLyricsIdx).GetValue<string>() : null;
                    var cellAlias = colAliasIdx != -1 ? row.Cell(colAliasIdx).GetValue<string>() : null;

                    long capturedId = await _libraryService.CaptureStagingAsync(new SongLibraryCaptureDto
                    {
                        Title = cellTitle.Trim(),
                        Artist = (cellArtist ?? "Unknown").Trim(),
                        YoutubeUrl = (cellYoutube ?? string.Empty).Trim(),
                        LyricsUrl = cellLyrics?.Trim(),
                        Alias = cellAlias?.Trim(),
                        SourceType = (int)MetadataSourceType.Streamer,
                        SourceId = streamerProfileId.ToString()
                    });

                    // [v19.5] 중복 체크: 제목과 가수가 완전히 일치하는 곡이 이미 활성 상태로 있는지 확인
                    var duplicateSong = existingSongs.FirstOrDefault(s => s.Title == cellTitle.Trim() && 
                                                                         s.Artist == (cellArtist ?? "Unknown").Trim() &&
                                                                         !s.IsDeleted);

                    if (duplicateSong != null)
                    {
                        // 중복 발견: 실제로 삭제 처리 (IsDeleted = true)
                        duplicateSong.IsDeleted = true;
                        duplicateSong.UpdatedAt = KstClock.Now;
                        
                        if (!result.Errors.Any(e => e.Contains("중복된 노래가 존재합니다")))
                        {
                            result.Errors.Add("중복된 노래가 존재합니다. 중복된 노래는 삭제됩니다. 삭제된 노래는 엑셀의 삭제된 노래 탭에서 확인 가능합니다.");
                        }
                    }

                    // 4. [SongNo 기반 업데이트 또는 신규 생성] - 중복 처리와 별개로 데이터 영속화 진행
                    FuncSongBooks? songBook = null;
                    var cellIdStr = colIdIdx != -1 ? row.Cell(colIdIdx).GetValue<string>() : null;

                    if (int.TryParse(cellIdStr, out int idValue))
                    {
                        songBook = existingSongs.FirstOrDefault(s => s.SongNo == idValue);
                    }
                    
                    if (songBook == null)
                    {
                        songBook = existingSongs.FirstOrDefault(s => s.SongLibraryId == capturedId);
                    }

                    if (songBook == null)
                    {
                        songBook = new FuncSongBooks
                        {
                            StreamerProfileId = streamerProfileId,
                            SongNo = int.TryParse(cellIdStr, out int newId) ? newId : ++maxSongNo,
                            CreatedAt = KstClock.Now
                        };
                        _db.TableFuncSongBooks.Add(songBook);
                    }

                    // 5. 정보 업데이트 및 활성화
                    songBook.SongLibraryId = capturedId;
                    songBook.Title = cellTitle.Trim();
                    songBook.Artist = (cellArtist ?? "Unknown").Trim();
                    
                    if (colCategoryIdx != -1) songBook.Category = row.Cell(colCategoryIdx).GetValue<string>()?.Trim();
                    if (colPitchIdx != -1) songBook.Pitch = row.Cell(colPitchIdx).GetValue<string>()?.Trim();
                    if (colProficiencyIdx != -1) songBook.Proficiency = row.Cell(colProficiencyIdx).GetValue<string>()?.Trim();
                    
                    songBook.ReferenceUrl = (cellYoutube ?? string.Empty).Trim();
                    songBook.LyricsUrl = cellLyrics?.Trim();
                    
                    if (colThumbnailIdx != -1) songBook.ThumbnailUrl = row.Cell(colThumbnailIdx).GetValue<string>()?.Trim();
                    if (colPointsIdx != -1) songBook.RequiredPoints = row.Cell(colPointsIdx).GetValue<int>();
                    
                    songBook.Alias = cellAlias?.Trim();
                    songBook.TitleChosung = KoreanUtils.NormalizeForSearch(cellTitle);
                    songBook.IsRequestable = true;
                    songBook.IsDeleted = false;
                    songBook.UpdatedAt = KstClock.Now;

                    processedSongNos.Add(songBook.SongNo);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"[{cellTitle}] 처리 중 오류: {ex.Message}");
                }
            }

            // 6. [오시리스의 정화]: 엑셀에 없는 기존 곡들은 삭제 처리
            var songsToDelete = existingSongs
                .Where(s => !s.IsDeleted && !processedSongNos.Contains(s.SongNo))
                .ToList();

            foreach (var song in songsToDelete)
            {
                song.IsDeleted = true;
                song.UpdatedAt = KstClock.Now;
            }

            await _db.SaveChangesAsync();

            // [오시리스의 예지]: 새로 등록되거나 업데이트된 곡 중 썸네일이 없는 곡들에 대해 비동기 수집 요청
            var songsToFetch = existingSongs
                .Where(s => !s.IsDeleted && processedSongNos.Contains(s.SongNo) && string.IsNullOrEmpty(s.ThumbnailUrl))
                .ToList();

            foreach (var song in songsToFetch)
            {
                await _mediator.Publish(new SongMetadataFetchEvent(song.Artist ?? "Unknown", song.Title, song.Id));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"엑셀 파일을 읽는 중 치명적인 오류가 발생했습니다: {ex.Message}");
        }

        return result;
    }
}
