using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Common;
using MooldangBot.Application.Common.Interfaces;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.Contracts.SongBook.Events;
using MediatR;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.SongBook.Events;

/// <summary>
/// [물멍]: SongMetadataFetchEvent를 처리하여 지능형 썸네일 수집 및 벡터화를 수행하는 핸들러입니다.
/// </summary>
public class SongMetadataFetchHandler : INotificationHandler<SongMetadataFetchEvent>
{
    private readonly IEnumerable<ISongThumbnailService> _thumbnailServices;
    private readonly IVectorEmbeddingService _embeddingService;
    private readonly IVectorSearchRepository _vectorRepository;
    private readonly IAppDbContext _db;
    private readonly IOverlayNotificationService _notificationService;

    public SongMetadataFetchHandler(
        IEnumerable<ISongThumbnailService> thumbnailServices,
        IVectorEmbeddingService embeddingService,
        IVectorSearchRepository vectorRepository,
        IAppDbContext db,
        IOverlayNotificationService notificationService)
    {
        _thumbnailServices = thumbnailServices;
        _embeddingService = embeddingService;
        _vectorRepository = vectorRepository;
        _db = db;
        _notificationService = notificationService;
    }

    public async Task Handle(SongMetadataFetchEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var artist = notification.Artist;
            var title = notification.Title;

            Console.WriteLine($"[MetadataFetch] 시작: {artist} - {title}");

            // 1. 벡터 임베딩 생성 (BGE-M3 로컬)
            var embedding = await _embeddingService.GetEmbeddingAsync($"{artist} {title}");

            // 2. 썸네일 수집 (엔진 체인 병렬 실행)
            var tasks = _thumbnailServices.Select(s => s.SearchThumbnailsAsync(artist, title));
            var resultsArrays = await Task.WhenAll(tasks);
            var thumbnailUrl = resultsArrays
                .Where(r => r != null)
                .SelectMany(r => r)
                .FirstOrDefault(url => !string.IsNullOrEmpty(url));

            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                Console.WriteLine($"[MetadataFetch] 썸네일을 찾지 못했습니다: {artist} - {title}");
                return;
            }

            // 3. 글로벌 메타데이터 적재 (Dapper)
            await _vectorRepository.SaveMetadataAsync(artist, title, thumbnailUrl, embedding);

            // 4. 로컬 노래책 정보 업데이트 (있는 경우)
            if (notification.SongBookId.HasValue)
            {
                var song = await _db.TableFuncSongBooks
                    .FirstOrDefaultAsync(s => s.Id == notification.SongBookId.Value, cancellationToken);

                if (song != null)
                {
                    song.ThumbnailUrl = thumbnailUrl;
                    song.UpdatedAt = KstClock.Now;

                    await _db.SaveChangesAsync(cancellationToken);
                    
                    // [오시리스의 영속]: [NotMapped] 컬럼이므로 Raw SQL을 통해 벡터 데이터 직접 저장
                    await _vectorRepository.UpdateSongVectorAsync(song.Id, embedding);
                    
                    Console.WriteLine($"[MetadataFetch] 노래책 업데이트 및 벡터화 완료: ID {song.Id}");

                    // 5. SignalR 알림 (대시보드 썸네일 갱신용)
                    await _notificationService.SendThumbnailUpdatedAsync(song.Id, thumbnailUrl);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MetadataFetch] 오류 발생: {ex.Message}");
        }
    }
}
