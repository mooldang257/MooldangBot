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
            .Select(s => new SongBookExcelRow
            {
                Id = s.SongNo,
                Title = s.Title,
                Artist = s.Artist,
                Category = s.Category,
                Pitch = s.Pitch,
                Proficiency = s.Proficiency,
                YoutubeUrl = s.ReferenceUrl ?? s.ThumbnailUrl, 
                LyricsUrl = s.LyricsUrl,
                ThumbnailUrl = s.ThumbnailUrl,
                RequiredPoints = s.RequiredPoints,
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
            // 1. 엑셀 데이터 읽기
            var rows = excelStream.Query<SongBookExcelRow>().ToList();
            result.TotalCount = rows.Count;

            // 2. 현재 스트리머의 곡 목록 미리 로드 (업데이트 및 SongNo 관리를 위해)
            var existingSongs = await _db.FuncSongBooks
                .Where(s => s.StreamerProfileId == streamerProfileId)
                .ToListAsync();

            var maxSongNo = existingSongs.Any() ? existingSongs.Max(s => s.SongNo) : 0;

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
                    bool isNew = false;

                    if (row.Id.HasValue)
                    {
                        // 엑셀에 Id(SongNo)가 명시된 경우 기존 곡 찾기
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
                        isNew = true;
                        _db.FuncSongBooks.Add(songBook);
                    }

                    // 5. 정보 업데이트
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
                    songBook.UpdatedAt = KstClock.Now;

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
