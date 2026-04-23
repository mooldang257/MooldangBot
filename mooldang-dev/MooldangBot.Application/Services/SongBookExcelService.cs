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
        var songs = await _db.SongBooks
            .AsNoTracking()
            .Where(s => s.StreamerProfileId == streamerProfileId && !s.IsDeleted)
            .OrderByDescending(s => s.Id)
            .Select(s => new SongBookExcelRow
            {
                Title = s.Title,
                Artist = s.Artist,
                Category = s.Category,
                Pitch = s.Pitch,
                Proficiency = s.Proficiency,
                YoutubeUrl = s.ReferenceUrl ?? s.ThumbnailUrl, // 유튜브 URL은 레퍼런스로 관리
                Alias = s.Alias
            })
            .ToListAsync();

        // 데이터가 없으면 헤더만 있는 빈 리스트 반환
        if (songs.Count == 0)
        {
            songs.Add(new SongBookExcelRow { Title = "예시: 노래 제목", Artist = "가수명" });
        }

        // 2. 메모리 스트림에 엑셀 저장
        var memoryStream = new MemoryStream();
        memoryStream.SaveAs(songs);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }

    public async Task<SongBookImportResultDto> ImportSongBookAsync(int streamerProfileId, Stream excelStream)
    {
        var result = new SongBookImportResultDto { TotalCount = 0, SuccessCount = 0 };
        
        try
        {
            // 1. 엑셀 데이터 읽기 (강력한 매핑)
            var rows = excelStream.Query<SongBookExcelRow>().ToList();
            result.TotalCount = rows.Count;

            foreach (var row in rows)
            {
                // [검증]: 제목은 무조건 있어야 함
                if (string.IsNullOrWhiteSpace(row.Title))
                {
                    result.Errors.Add($"{result.TotalCount}번째 행: 제목이 없습니다. 건너뜁니다.");
                    continue;
                }

                try
                {
                    // 2. [지능형 매칭 Phase]: SongLibraryId 확보
                    // 제목/가수를 기반으로 마스터 DB에서 최적의 라이브러리 ID를 징집합니다.
                    long capturedId = await _libraryService.CaptureStagingAsync(new SongLibraryCaptureDto
                    {
                        Title = row.Title.Trim(),
                        Artist = row.Artist?.Trim() ?? "Unknown",
                        YoutubeUrl = row.YoutubeUrl?.Trim() ?? string.Empty,
                        Alias = row.Alias,
                        SourceType = (int)MetadataSourceType.Streamer,
                        SourceId = streamerProfileId.ToString()
                    });

                    // 3. [중복 체크]: 이미 노래책에 동일한 LibraryId가 있는지 확인
                    var isExists = await _db.SongBooks
                        .AnyAsync(s => s.StreamerProfileId == streamerProfileId && 
                                       s.SongLibraryId == capturedId && 
                                       !s.IsDeleted);

                    if (isExists)
                    {
                        result.Errors.Add($"[{row.Title}]: 이미 노래책에 등록된 곡입니다.");
                        continue;
                    }

                    // 4. [엔터티 생성 및 저장]
                    var songBook = new SongBook
                    {
                        StreamerProfileId = streamerProfileId,
                        SongLibraryId = capturedId,
                        Title = row.Title.Trim(),
                        Artist = row.Artist?.Trim(),
                        Category = row.Category?.Trim(),
                        Pitch = row.Pitch?.Trim(),
                        Proficiency = row.Proficiency?.Trim(),
                        ReferenceUrl = row.YoutubeUrl?.Trim(),
                        Alias = row.Alias?.Trim(),
                        TitleChosung = KoreanUtils.NormalizeForSearch(row.Title),
                        IsRequestable = true,
                        CreatedAt = KstClock.Now
                    };

                    _db.SongBooks.Add(songBook);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"[{row.Title}] 처리 중 오류: {ex.Message}");
                }
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
