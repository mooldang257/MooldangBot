using MooldangBot.Modules.SongBook.Events;
using MooldangBot.Modules.SongBook.State;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace MooldangBot.Modules.SongBook.Features.Commands;

/// <summary>
/// [곡 신청 명령]: 시청자가 노래를 신청할 때 처리되는 로직입니다.
/// (v15.1: 모듈화 및 이벤트 기반 알림으로 전환되었습니다.)
/// </summary>
public record AddSongRequestCommand(string ChzzkUid, string Username, string SongTitle, int DonationAmount = 0, int DefaultCost = 0) : IRequest<Result<bool>>;

public class AddSongRequestCommandHandler(
    SongBookState state, 
    IMediator mediator,
    ISongBookDbContext db,
    ISongBookRepository repository,
    ILlmService llmService,
    IOverlayNotificationService overlayNotification) : IRequestHandler<AddSongRequestCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AddSongRequestCommand request, CancellationToken ct)
    {
        var title = request.SongTitle;
        var artist = "";

        // 1. 스트리머 프로필 확인
        var profile = await db.CoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == request.ChzzkUid, ct);

        if (profile == null) return Result<bool>.Failure("스트리머 프로필을 찾을 수 없습니다.");

        // 🎯 AI 벡터 생성
        float[]? vector = null;
        try {
            vector = await llmService.GetEmbeddingAsync(request.SongTitle);
        } catch { /* AI 장애 시 텍스트 검색으로만 진행 */ }

        // [v19.0]: 개인 노래책(SongBook)에서 우선 수색
        var personalResults = await repository.SearchPersonalSongBookAsync(profile.Id, request.SongTitle, vector, limit: 1);
        var matchedSong = personalResults.FirstOrDefault();

        string? thumbnailUrl = null;
        string? pitch = null;
        int requiredPoints = request.DefaultCost; // 기본값은 명령어 설정 비용

        if (matchedSong != null)
        {
            title = matchedSong.Title;
            artist = matchedSong.Artist ?? "";
            pitch = matchedSong.Pitch;
            thumbnailUrl = matchedSong.ThumbnailUrl;
            requiredPoints = matchedSong.RequiredPoints; // 노래책 전용 비용 적용

            if (matchedSong.SongLibraryId.HasValue && string.IsNullOrEmpty(thumbnailUrl))
            {
                var master = await db.FuncMasterSongLibraries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.SongLibraryId == matchedSong.SongLibraryId.Value, ct);
                thumbnailUrl ??= master?.ThumbnailUrl;
            }
        }
        else 
        {
            var libraryResults = await repository.SearchStreamerSongsAsync(profile.Id, request.SongTitle, vector, limit: 1);
            var libSong = libraryResults.FirstOrDefault();
            if (libSong != null)
            {
                title = libSong.Title;
                artist = libSong.Artist ?? "";
                var master = await db.FuncMasterSongLibraries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.SongLibraryId == libSong.SongLibraryId, ct);
                thumbnailUrl = master?.ThumbnailUrl;
            }
        }

        // [v23.0] 비용 검증 및 누적 로직
        if (requiredPoints > 0)
        {
            // 기존 누적액 확인 (노래책에 있는 곡만 누적 지원)
            int accumulated = 0;
            SongAccumulation? existingAcc = null;

            if (matchedSong != null)
            {
                existingAcc = await db.SongAccumulations
                    .FirstOrDefaultAsync(a => a.StreamerProfileId == profile.Id && a.SongBookId == matchedSong.Id, ct);
                accumulated = existingAcc?.CurrentPoints ?? 0;
            }

            int totalDonation = accumulated + request.DonationAmount;

            if (totalDonation < requiredPoints)
            {
                // [부족]: 누적 처리
                if (matchedSong != null)
                {
                    if (existingAcc == null)
                    {
                        existingAcc = new SongAccumulation
                        {
                            StreamerProfileId = profile.Id,
                            SongBookId = matchedSong.Id,
                            CurrentPoints = request.DonationAmount,
                            LastDonatorName = request.Username,
                            CreatedAt = KstClock.Now
                        };
                        db.SongAccumulations.Add(existingAcc);
                    }
                    else
                    {
                        existingAcc.CurrentPoints += request.DonationAmount;
                        existingAcc.LastDonatorName = request.Username;
                        existingAcc.UpdatedAt = KstClock.Now;
                    }
                    await db.SaveChangesAsync(ct);
                    
                    return Result<bool>.Failure($"신청 비용이 부족합니다. (현재 {existingAcc.CurrentPoints}/{requiredPoints} 치즈 누적 중 🧀)");
                }
                else
                {
                    return Result<bool>.Failure($"노래책에 없는 곡은 최소 {requiredPoints} 치즈 후원이 필요합니다.");
                }
            }
            else
            {
                // [성공]: 누적 데이터 삭제 (있다면)
                if (existingAcc != null)
                {
                    db.SongAccumulations.Remove(existingAcc);
                    // Save는 아래 전체 저장 시 함께 처리
                }
            }
        }

        // 3. [영속화]: DB에 신청 내역 저장
        var queueCount = await db.FuncSongQueues
            .Where(q => q.StreamerProfileId == profile.Id)
            .CountAsync(ct);

        var finalTitle = string.IsNullOrEmpty(artist) ? title : $"{title} - {artist}";

        var newRequest = new SongQueue
        {
            StreamerProfileId = profile.Id,
            RequesterNickname = request.Username,
            Title = finalTitle,
            Status = SongStatus.Pending,
            CreatedAt = KstClock.Now,
            SortOrder = queueCount + 1,
            VideoId = matchedSong?.ReferenceUrl, 
            ThumbnailUrl = thumbnailUrl,
            Pitch = pitch
        };

        db.FuncSongQueues.Add(newRequest);
        await db.SaveChangesAsync(ct);
        int newSongId = newRequest.Id;

        // 인메모리 버퍼 실제 추가
        var added = state.AddSong(request.ChzzkUid, newSongId, request.Username, title, artist, matchedSong?.ReferenceUrl, thumbnailUrl, pitch);
        
        if (!added)
            return Result<bool>.Failure("이미 신청된 곡입니다.");

        // 4. 이벤트 발행 및 오버레이 알림
        await mediator.Publish(new SongAddedEvent(request.Username, title, request.ChzzkUid), ct);

        var current = state.GetCurrentSong(request.ChzzkUid);
        var queue = state.GetQueue(request.ChzzkUid)
            .Select(s => new QueueSongDto(s.Id, s.Title, s.Artist, s.Username, s.VideoId, s.ThumbnailUrl, s.Pitch))
            .ToList();
        
        var overlayData = new SongOverlayDto(
            current != null ? new CurrentSongDto(current.Id, current.Title, current.Artist, current.VideoId, current.ThumbnailUrl, current.Pitch) : null,
            queue,
            new SongOverlaySettings()
        );

        await overlayNotification.NotifySongOverlayUpdateAsync(request.ChzzkUid, overlayData, ct);

        return Result<bool>.Success(true);
    }

    private string? ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        
        // 간단한 유튜브 ID 추출 로직 (v=... 또는 youtu.be/...)
        if (url.Contains("v="))
        {
            return url.Split("v=")[1].Split("&")[0];
        }
        else if (url.Contains("youtu.be/"))
        {
            return url.Split("youtu.be/")[1].Split("?")[0];
        }

        return null;
    }
}
