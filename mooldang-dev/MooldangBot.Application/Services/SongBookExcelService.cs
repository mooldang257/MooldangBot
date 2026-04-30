using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Application.Common.Utils;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Services;

/// <summary>
/// [v19.0] MiniExcel 기반 노래책 고속 일괄 처리 구현체 (High-Speed Ingestion)
/// </summary>
public class SongBookExcelService(
    IAppDbContext dbContext,
    ISongLibraryService libraryService) : ISongBookExcelService
{
    private readonly IAppDbContext _db = dbContext;
    private readonly ISongLibraryService _libraryService = libraryService;

    public async Task<Stream> ExportSongBookAsync(int streamerProfileId)
    {
        // 1. 현재 노래책 데이터 조회 (가장 최근 등록순)
        var songs = await _db.FuncSongBooks
            .AsNoTracking()
            .Where(s => s.StreamerProfileId == streamerProfileId && !s.IsDeleted)
            .OrderBy(s => s.SongNo)
            .ToListAsync();

        // 2. ClosedXML을 사용하여 엑셀 생성 (콤보박스 지원을 위함)
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("노래책");

        // 헤더 설정
        var headers = new[] { "Id", "Title", "Artist", "Category", "Pitch", "Proficiency", "Youtube", "Lyrics", "Thumbnail", "Points", "Alias" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        // 데이터 채우기
        for (int i = 0; i < songs.Count; i++)
        {
            var song = songs[i];
            int row = i + 2;
            worksheet.Cell(row, 1).Value = song.SongNo;
            worksheet.Cell(row, 2).Value = song.Title;
            worksheet.Cell(row, 3).Value = song.Artist;
            worksheet.Cell(row, 4).Value = song.Category;
            worksheet.Cell(row, 5).Value = song.Pitch ?? "원키";
            worksheet.Cell(row, 6).Value = song.Proficiency ?? "완창";
            worksheet.Cell(row, 7).Value = song.ReferenceUrl;
            worksheet.Cell(row, 8).Value = song.LyricsUrl;
            worksheet.Cell(row, 9).Value = song.ThumbnailUrl;
            worksheet.Cell(row, 10).Value = song.RequiredPoints;
            worksheet.Cell(row, 11).Value = song.Alias;
        }

        // 3. [오시리스의 편의]: 콤보박스(데이터 유효성 검사) 설정
        // Pitch (E열)
        var pitchList = new[] { "원키", "-6", "-5", "-4", "-3", "-2", "-1", "+1", "+2", "+3", "+4", "+5", "+6" };
        var pitchRange = worksheet.Range(2, 5, 2000, 5); // 최대 2000행까지 지원
        pitchRange.CreateDataValidation().List(string.Join(",", pitchList));

        // Proficiency (F열)
        var profList = new[] { "완창", "1절", "연습중", "구걸가능" };
        var profRange = worksheet.Range(2, 6, 2000, 6);
        profRange.CreateDataValidation().List(string.Join(",", profList));

        // 4. 스타일링 및 마무리
        var headerRow = worksheet.FirstRow();
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E0F2FE"); // Sky 100
        headerRow.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        worksheet.Columns().AdjustToContents();
        worksheet.Column(2).Width = 30; // Title 컬럼은 조금 더 넓게

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
            // 1. 엑셀 데이터 읽기
            var rows = excelStream.Query<SongBookExcelRow>().ToList();
            result.TotalCount = rows.Count;

            // 2. 현재 스트리머의 곡 목록 미리 로드 (업데이트 및 SongNo 관리를 위해)
            var existingSongs = await _db.FuncSongBooks
                .Where(s => s.StreamerProfileId == streamerProfileId)
                .ToListAsync();

            var maxSongNo = existingSongs.Any() ? existingSongs.Max(s => s.SongNo) : 0;
            var processedSongNos = new HashSet<int>();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Title))
                {
                    result.Errors.Add($"{result.TotalCount}번째 행: 제목이 없습니다. 건너뜁니다.");
                    continue;
                }

                try
                {
                    // 3. 라이브러리 ID 확보 (지능형 매칭)
                    long capturedId = await _libraryService.CaptureStagingAsync(new SongLibraryCaptureDto
                    {
                        Title = row.Title.Trim(),
                        Artist = row.Artist?.Trim() ?? "Unknown",
                        YoutubeUrl = row.YoutubeUrl?.Trim() ?? string.Empty,
                        LyricsUrl = row.LyricsUrl?.Trim(),
                        Alias = row.Alias,
                        SourceType = (int)MetadataSourceType.Streamer,
                        SourceId = streamerProfileId.ToString()
                    });

                    // 4. [SongNo 기반 업데이트 또는 신규 생성]
                    SongBook? songBook = null;

                    if (row.Id.HasValue)
                    {
                        // 엑셀에 Id(SongNo)가 명시된 경우 기존 곡 찾기 (삭제된 곡 포함)
                        songBook = existingSongs.FirstOrDefault(s => s.SongNo == (int)row.Id.Value);
                    }
                    
                    if (songBook == null)
                    {
                        // 기존 곡이 없으면 중복 체크 (LibraryId 기준)
                        songBook = existingSongs.FirstOrDefault(s => s.SongLibraryId == capturedId);
                    }

                    if (songBook == null)
                    {
                        // 완전히 새로운 곡 생성
                        songBook = new SongBook
                        {
                            StreamerProfileId = streamerProfileId,
                            SongNo = row.Id.HasValue ? (int)row.Id.Value : ++maxSongNo,
                            CreatedAt = KstClock.Now
                        };
                        _db.FuncSongBooks.Add(songBook);
                    }

                    // 5. 정보 업데이트 및 활성화
                    songBook.SongLibraryId = capturedId;
                    songBook.Title = row.Title.Trim();
                    songBook.Artist = row.Artist?.Trim();
                    songBook.Category = row.Category?.Trim();
                    songBook.Pitch = row.Pitch?.Trim();
                    songBook.Proficiency = row.Proficiency?.Trim();
                    songBook.ReferenceUrl = row.YoutubeUrl?.Trim();
                    songBook.LyricsUrl = row.LyricsUrl?.Trim();
                    songBook.ThumbnailUrl = row.ThumbnailUrl?.Trim();
                    songBook.RequiredPoints = row.RequiredPoints ?? 0;
                    songBook.Alias = row.Alias?.Trim();
                    songBook.TitleChosung = KoreanUtils.NormalizeForSearch(row.Title);
                    songBook.IsRequestable = true;
                    songBook.IsDeleted = false; // 엑셀에 있으면 활성화
                    songBook.UpdatedAt = KstClock.Now;

                    processedSongNos.Add(songBook.SongNo);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"[{row.Title}] 처리 중 오류: {ex.Message}");
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
        }
        catch (Exception ex)
        {
            result.Errors.Add($"엑셀 파일을 읽는 중 치명적인 오류가 발생했습니다: {ex.Message}");
        }

        return result;
    }
}
