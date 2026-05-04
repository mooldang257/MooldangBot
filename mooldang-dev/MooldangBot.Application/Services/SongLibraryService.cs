using Microsoft.EntityFrameworkCore;
using Dapper;
using FuzzySharp;
using MooldangBot.Application.Common.Utils;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MooldangBot.Application.Services;

/// <summary>
/// [v13.0] 중앙 병기창 하이브리드 검색 및 정밀 징집 구현체 (Linguistic Resonance)
/// </summary>
public class SongLibraryService(
    IAppDbContext dbContext, 
    IYouTubeSearchService youtubeService, 
    ISongLibraryIdGenerator idGenerator,
    ILlmService llmService,
    CommandBackgroundTaskQueue taskQueue,
    AdaptiveAiRateLimiter rateLimiter,
    IServiceProvider serviceProvider) : ISongLibraryService
{
    private readonly IAppDbContext _context = dbContext;
    private readonly IYouTubeSearchService _youtube = youtubeService;
    private readonly ISongLibraryIdGenerator _idGenerator = idGenerator; // [v13.1] Snowflake ID 생성기 주입
    private readonly ILlmService _llm = llmService; // [v18.0] AI 신경망 주입
    private readonly CommandBackgroundTaskQueue _taskQueue = taskQueue; // [v18.1] 백그라운드 큐 오프로딩
    private readonly AdaptiveAiRateLimiter _rateLimiter = rateLimiter; // [v18.1] 지능형 속도 제한
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<List<SongLibrarySearchResultDto>> SearchLibraryAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SongLibrarySearchResultDto>();

        // 🔍 [정규화]: 검색어 전처리 (공백 제거, 소문자화, 초성 추출)
        string normalizedQuery = KoreanUtils.NormalizeForSearch(query);
        string rawQuery = query.ToLowerInvariant().Trim();

        // 🚀 [1차 - 내부 병기창(DB)]: 제목, 별칭, 초성 필터링 (최우선 순위)
        var candidates = await _context.TableFuncSongMasterLibrary
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
            var existing = await _context.TableFuncSongMasterStaging
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

        var staging = new FuncSongMasterStaging
        {
            SongLibraryId = newLibraryId,
            Title = dto.Title,
            Artist = dto.Artist,
            TitleChosung = KoreanUtils.NormalizeForSearch(dto.Title), // [v13.1] 초성 필터링 데이터 보강
            ArtistChosung = KoreanUtils.NormalizeForSearch(dto.Artist),
            Alias = string.IsNullOrWhiteSpace(aliasString) ? null : aliasString,
            YoutubeUrl = dto.YoutubeUrl,
            YoutubeTitle = dto.YoutubeTitle,
            LyricsUrl = dto.LyricsUrl,
            SourceType = (MetadataSourceType)dto.SourceType,
            SourceId = dto.SourceId,
            CreatedAt = KstClock.Now
        };

        // 4. [v18.1] AI 메타데이터 보강 (백그라운드 오프로딩 + 가변 지연)
        // 시청자 응답 속도 향상을 위해 별칭 및 벡터 생성은 백그라운드에서 처리합니다.
        _ = _taskQueue.QueueBackgroundWorkItemAsync(async ct => 
        {
            await EnrichWithAiMetadataInBackgroundAsync(newLibraryId);
        });

        _context.TableFuncSongMasterStaging.Add(staging);
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
        var staging = await _context.TableFuncSongMasterStaging
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
            if (dto.LyricsUrl != null) staging.LyricsUrl = dto.LyricsUrl;
            
            staging.CreatedAt = KstClock.Now; // 수명 연장

            // [v18.1] 제목이 바뀌었다면 AI 메타데이터 재생성 (백그라운드)
            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                _ = _taskQueue.QueueBackgroundWorkItemAsync(async ct => 
                {
                    await EnrichWithAiMetadataInBackgroundAsync(libraryIdToUse);
                });
            }
        }
        else
        {
            // 3-B. [Insert]: 정보가 없으면 신규 생성 (Auto-Recovery)
            staging = new FuncSongMasterStaging
            {
                SongLibraryId = libraryIdToUse,
                Title = dto.Title,
                Artist = dto.Artist,
                TitleChosung = KoreanUtils.NormalizeForSearch(dto.Title),
                ArtistChosung = KoreanUtils.NormalizeForSearch(dto.Artist),
                YoutubeUrl = dto.YoutubeUrl,
                LyricsUrl = dto.LyricsUrl,
                SourceType = (MetadataSourceType)dto.SourceType,
                SourceId = dto.SourceId ?? "system_recovery",
                CreatedAt = KstClock.Now
            };

            // [v18.1] AI 메타데이터 보강 (백그라운드)
            _ = _taskQueue.QueueBackgroundWorkItemAsync(async ct => 
            {
                await EnrichWithAiMetadataInBackgroundAsync(libraryIdToUse);
            });

            _context.TableFuncSongMasterStaging.Add(staging);
        }

        await _context.SaveChangesAsync();
        return libraryIdToUse;
    }

    /// <summary>
    /// [v18.1] 백그라운드에서 안전하게 AI 보강 작업을 수행합니다. (지능형 리미터 적용)
    /// </summary>
    private async Task EnrichWithAiMetadataInBackgroundAsync(long libraryId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var llm = scope.ServiceProvider.GetRequiredService<ILlmService>();
        var limiter = scope.ServiceProvider.GetRequiredService<AdaptiveAiRateLimiter>();

        var staging = await db.TableFuncSongMasterStaging.FirstOrDefaultAsync(s => s.SongLibraryId == libraryId);
        if (staging == null) return;

        try 
        {
            // 1. 속도 제한 확인 및 획득 (RPM 15 한도 도달 시 여기서 대기)
            await limiter.AcquireAsync();

            // 2. 벡터 데이터 생성
            string embeddingText = $"{staging.Title} - {staging.Artist}";
            var vector = await llm.GetEmbeddingAsync(embeddingText);
            if (vector != null && vector.Length > 0)
            {
                staging.TitleVector = vector; // 메모리 상에만 유지
                
                // [v11.7-Fix] EF Core에서 매핑을 제거했으므로 수동으로(Dapper) 업데이트합니다.
                var binaryVector = new byte[vector.Length * 4];
                Buffer.BlockCopy(vector, 0, binaryVector, 0, binaryVector.Length);
                
                await db.Database.GetDbConnection().ExecuteAsync(
                    "UPDATE FuncSongMasterStaging SET TitleVector = @vector WHERE Id = @id",
                    new { vector = binaryVector, id = staging.Id });
            }

            // 3. AI 기반 별칭 생성
            var systemPrompt = "당신은 음악 전문가입니다. 노래 제목을 받으면 한국 사람들이 흔히 부를 법한 줄임말이나 별칭을 생성하세요. " +
                               "답변은 반드시 별칭들만 쉼표로 구분하여 출력하고, 다른 설명은 하지 마세요. (예: '우리는 언젠가 죽어요' -> '우언죽, 언젠가죽어요')";
            
            var aiAlias = await llm.GenerateResponseAsync(systemPrompt, staging.Title);
            if (!string.IsNullOrWhiteSpace(aiAlias))
            {
                var existingAlias = staging.Alias ?? "";
                var finalAlias = (existingAlias + (string.IsNullOrWhiteSpace(existingAlias) ? "" : ", ") + aiAlias).Trim(',', ' ');
                staging.Alias = finalAlias;
            }

            await db.SaveChangesAsync();
        }
        catch (Exception)
        {
            // 백그라운드 작업이므로 예외는 로깅만 하고 종료
        }
    }
}
