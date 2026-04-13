using Microsoft.EntityFrameworkCore;
using FuzzySharp;
using MooldangBot.Application.Common.Utils;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;

namespace MooldangBot.Application.Services;

/// <summary>
/// [v13.0] 중앙 병기창 하이브리드 검색 및 정밀 징집 구현체 (Linguistic Resonance)
/// </summary>
public class SongLibraryService(IAppDbContext dbContext, IYouTubeSearchService youtubeService, ISongLibraryIdGenerator idGenerator) : ISongLibraryService
{
    private readonly IAppDbContext _context = dbContext;
    private readonly IYouTubeSearchService _youtube = youtubeService;
    private readonly ISongLibraryIdGenerator _idGenerator = idGenerator; // [v13.1] Snowflake ID 생성기 주입

    public async Task<List<SongLibrarySearchResultDto>> SearchLibraryAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SongLibrarySearchResultDto>();

        // 🔍 [정규화]: 검색어 전처리 (공백 제거, 소문자화, 초성 추출)
        string normalizedQuery = KoreanUtils.NormalizeForSearch(query);
        string rawQuery = query.ToLowerInvariant().Trim();

        // 🚀 [1차 - 내부 병기창(DB)]: 제목, 별칭, 초성 필터링 (최우선 순위)
        var candidates = await _context.MasterSongLibraries
            .Where(s => s.Title.Contains(rawQuery) || 
                        (s.Alias != null && s.Alias.Contains(rawQuery)) ||
                        (s.TitleChosung != null && s.TitleChosung.Contains(normalizedQuery)))
            .Take(50) 
            .ToListAsync();

        // 🎯 [내부 유사도 매칭 (FuzzySharp)] - 상위 5개만 선별
        var fuzzyResults = Process.ExtractSorted(
            rawQuery, 
            candidates.Select(c => $"{c.Title} {c.Artist} {c.Alias}".ToLower()), 
            s => s, 
            cutoff: 40 // [v13.0] 더 넓은 수색을 위해 컷오프 소폭 하향
        ).Take(5); 

        var finalResults = fuzzyResults
            .Select(f => new SongLibrarySearchResultDto
            {
                Song = candidates.ElementAt(f.Index),
                Score = f.Score
            })
            .ToList();

        // 🛰️ [2차 - 유튜브 실시간 정찰]: 
        // 내부 결과가 5개 미만인 경우, 부족한 만큼 유튜브에서 정찰 데이터를 보충합니다.
        int youtubeLimit = 5; // [물멍]: 유튜브는 항상 5개까지 빵빵하게 보충
        var youtubeResults = await _youtube.SearchVideosAsync(query, limit: youtubeLimit);
        
        finalResults.AddRange(youtubeResults.Select(y => new SongLibrarySearchResultDto
        {
            ExternalSong = y,
            Score = 40 // 유튜브 결과는 보조 데이터로서 기본 점수 부여
        }));

        // 최종적으로 상위 8개만 엄선하여 보고 (중복 제거 및 최적화 가능)
        return finalResults.Take(8).ToList();
    }

    // [v13.1] 지능형 멱등성 병기창 징집 (Linguistic Resonance + Snowflake)
    public async Task<long> CaptureStagingAsync(SongLibraryCaptureDto dto)
    {
        // 1. [v13.1] 멱등성 확인 (최근 1개월 내 동일 URL 존재 여부)
        if (!string.IsNullOrWhiteSpace(dto.YoutubeUrl))
        {
            var existing = await _context.MasterSongStagings
                .FirstOrDefaultAsync(s => s.YoutubeUrl == dto.YoutubeUrl);
            if (existing != null) return existing.SongLibraryId;
        }

        // 2. [v13.1] 지능형 별칭 및 초성 가공
        var smartAliases = KoreanUtils.ExtractSmartAliases(dto.YoutubeTitle, dto.Title, dto.Artist);
        var manualAliases = (dto.Alias ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var finalAliases = smartAliases.Union(manualAliases, StringComparer.OrdinalIgnoreCase).ToList();
        var aliasString = string.Join(", ", finalAliases);

        // 3. [v13.1] 신규 전역 ID 발급
        var newLibraryId = _idGenerator.GenerateNewId();

        var staging = new Master_SongStaging
        {
            SongLibraryId = newLibraryId,
            Title = dto.Title,
            Artist = dto.Artist,
            TitleChosung = KoreanUtils.NormalizeForSearch(dto.Title), // [v13.1] 초성 필터링 데이터 보강
            ArtistChosung = KoreanUtils.NormalizeForSearch(dto.Artist),
            Alias = string.IsNullOrWhiteSpace(aliasString) ? null : aliasString,
            YoutubeUrl = dto.YoutubeUrl,
            YoutubeTitle = dto.YoutubeTitle,
            Lyrics = dto.Lyrics,
            SourceType = (MetadataSourceType)dto.SourceType,
            SourceId = dto.SourceId,
            CreatedAt = KstClock.Now
        };

        _context.MasterSongStagings.Add(staging);
        await _context.SaveChangesAsync();

        return newLibraryId;
    }

    // [v13.1] 지능형 데이터 복구 및 업데이트 (Auto-Recovery + Upsert)
    public async Task<long> UpdateStagingAsync(long currentLibraryId, SongLibraryCaptureDto dto)
    {
        long libraryIdToUse = currentLibraryId;

        // 1. [v13.1] 레거시 데이터 구제 (ID 없으면 새로 발급)
        if (libraryIdToUse <= 0)
        {
            libraryIdToUse = _idGenerator.GenerateNewId();
        }

        // 2. 기존 Staging 조회
        var staging = await _context.MasterSongStagings
            .FirstOrDefaultAsync(s => s.SongLibraryId == libraryIdToUse);

        if (staging != null)
        {
            // 3-A. [Update]: 이미 존재하면 덮어쓰기 (초성 갱신 포함)
            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                staging.Title = dto.Title;
                staging.TitleChosung = KoreanUtils.NormalizeForSearch(dto.Title);
            }
            if (dto.Artist != null)
            {
                staging.Artist = dto.Artist;
                staging.ArtistChosung = KoreanUtils.NormalizeForSearch(dto.Artist);
            }
            if (dto.YoutubeUrl != null) staging.YoutubeUrl = dto.YoutubeUrl;
            if (dto.Lyrics != null) staging.Lyrics = dto.Lyrics;
            
            staging.CreatedAt = KstClock.Now; // 수명 연장
        }
        else
        {
            // 3-B. [Insert]: 정보가 없으면 신규 생성 (Auto-Recovery)
            staging = new Master_SongStaging
            {
                SongLibraryId = libraryIdToUse,
                Title = dto.Title,
                Artist = dto.Artist,
                TitleChosung = KoreanUtils.NormalizeForSearch(dto.Title),
                ArtistChosung = KoreanUtils.NormalizeForSearch(dto.Artist),
                YoutubeUrl = dto.YoutubeUrl,
                Lyrics = dto.Lyrics,
                SourceType = (MetadataSourceType)dto.SourceType,
                SourceId = dto.SourceId ?? "system_recovery",
                CreatedAt = KstClock.Now
            };
            _context.MasterSongStagings.Add(staging);
        }

        await _context.SaveChangesAsync();
        return libraryIdToUse;
    }
}
