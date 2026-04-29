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
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MooldangBot.Modules.SongBook.Features.Commands;

/// <summary>
/// [곡 신청 명령]: 시청자가 노래를 신청할 때 처리되는 로직입니다.
/// </summary>
public record AddSongRequestCommand(
    string StreamerUid,
    string SenderId,
    string Username,
    string SongTitle,
    int DonationAmount = 0,
    int RequiredPoints = 0
) : IRequest<Result<string>>;

public class AddSongRequestCommandHandler(
    SongBookState state, 
    IMediator mediator,
    ISongBookDbContext db,
    ISongBookRepository repository,
    ILlmService llmService,
    IOverlayNotificationService overlayNotification,
    IIdentityCacheService identityCache,
    ILogger<AddSongRequestCommandHandler> logger) : IRequestHandler<AddSongRequestCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddSongRequestCommand request, CancellationToken ct)
    {
        logger.LogInformation("🚀 [SongRequest] Starting request processing: {Title} from {User} (Donation: {Donation})", request.SongTitle, request.Username, request.DonationAmount);

        // 1. [식별]: 스트리머 및 시청자 정보 로드
        var profile = await db.CoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == request.StreamerUid, ct);

        if (profile == null) return Result<string>.Failure("스트리머 정보를 찾을 수 없습니다.");
        logger.LogInformation("🔍 [SongRequest] Repo Type: {RepoType}", repository.GetType().FullName);

        float[]? vector = null;
        try 
        {
            vector = await llmService.GetEmbeddingAsync(request.SongTitle);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ [SongRequest] Failed to get embedding for vector search.");
        }

        // [v19.0]: 개인 노래책(SongBook)에서 우선 수색
        var personalResults = await repository.SearchPersonalSongBookAsync(profile.Id, request.SongTitle, vector, limit: 1);
        var matchedSong = personalResults.FirstOrDefault();
        
        string finalTitle;
        string? artist = null;
        string? thumbnailUrl = null;
        string? pitch = null;
        int requiredPoints = request.RequiredPoints;

        if (matchedSong != null)
        {
            finalTitle = matchedSong.Title;
            artist = matchedSong.Artist;
            thumbnailUrl = matchedSong.ThumbnailUrl;
            pitch = matchedSong.Pitch;
            requiredPoints = matchedSong.RequiredPoints;
            logger.LogInformation("🎯 [SongRequest] Match Found in SongBook: {Title} (Cost: {Points} Cheese)", finalTitle, requiredPoints);
        }
        else
        {
            logger.LogInformation("❓ [SongRequest] No match found. Attempting AI Title Extraction for '{Input}'", request.SongTitle);
            try
            {
                var prompt = $@"Extract the song title and artist from the following request: ""{request.SongTitle}""
Return as JSON: {{ ""title"": ""..."", ""artist"": ""..."" }}
If you can't find it, use the original text as title.";
                var extracted = await llmService.GenerateResponseAsync(prompt, "");
                logger.LogInformation("🤖 [SongRequest] AI Raw Result: {Result}", extracted);

                using var doc = JsonDocument.Parse(extracted);
                finalTitle = doc.RootElement.GetProperty("title").GetString() ?? request.SongTitle;
                artist = doc.RootElement.TryGetProperty("artist", out var a) ? a.GetString() : null;
            }
            catch (Exception ex)
            {
                logger.LogWarning("⚠️ [SongRequest] AI Extraction failed: {Msg}. Using raw title.", ex.Message);
                finalTitle = request.SongTitle;
            }
        }

        logger.LogInformation("📝 [SongRequest] Final Title for Queue: {Title}", finalTitle);

        // 2. [결제]: 포인트 차감 처리
        if (request.DonationAmount < requiredPoints)
        {
            int needed = requiredPoints - request.DonationAmount;
            logger.LogInformation("💳 [SongRequest] Points deduction: {Needed}", needed);
            
            var deductResult = await mediator.Send(new MooldangBot.Modules.Point.Requests.Commands.DeductDonationPointsCommand(
                request.StreamerUid,
                request.SenderId,
                needed
            ), ct);

            if (!deductResult.Success)
            {
                logger.LogWarning("⚠️ [SongRequest] Insufficient balance for {User}: Needs {Needed}", request.Username, needed);
                return Result<string>.Failure($"포인트가 부족합니다. (현재 잔액: {deductResult.CurrentBalance} / 필요: {needed} 치즈 🧀)");
            }
        }

        // 3. [영속화]: DB에 신청 내역 저장
        try 
        {
            var queueCount = await db.FuncSongQueues
                .Where(q => q.StreamerProfileId == profile.Id && !q.IsDeleted)
                .CountAsync(ct);

            var newRequest = new SongQueue
            {
                StreamerProfileId = profile.Id,
                GlobalViewerId = await ResolveGlobalViewerIdAsync(request.SenderId, request.Username, ct),
                RequesterNickname = request.Username,
                Title = finalTitle,
                Artist = artist,
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
            logger.LogInformation("✅ [SongRequest] Successfully saved to DB (ID: {Id})", newSongId);

            // 인메모리 버퍼 실제 추가
            var added = state.AddSong(request.StreamerUid, newSongId, request.Username, finalTitle, artist, matchedSong?.ReferenceUrl, thumbnailUrl, pitch);
            if (!added) logger.LogWarning("⚠️ [SongRequest] Failed to add to in-memory state.");
            
            // 4. [알림]: 오버레이 업데이트 통지
            var current = state.GetCurrentSong(request.StreamerUid);
            var queue = state.GetQueue(request.StreamerUid)
                .Select(s => new QueueSongDto(s.Id, s.Title, s.Artist, s.Username, s.VideoId, s.ThumbnailUrl, s.Pitch))
                .ToList();
            
            var overlayData = new SongOverlayDto(
                current != null ? new CurrentSongDto(current.Id, current.Title, current.Artist, current.VideoId, current.ThumbnailUrl, current.Pitch) : null,
                queue,
                new SongOverlaySettings()
            );

            await overlayNotification.NotifySongOverlayUpdateAsync(request.StreamerUid, overlayData, ct);
            await overlayNotification.NotifySongQueueChangedAsync(request.StreamerUid, ct);
            await overlayNotification.NotifyPointChangedAsync(request.StreamerUid, ct);
            logger.LogInformation("📡 [SongRequest] All notifications (Overlay, Queue, Dashboard) sent.");

            return Result<string>.Success(finalTitle);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ [SongRequest] Persistence failed. (Refund process starting)");
            // 환불 로직
            if (request.DonationAmount < requiredPoints)
            {
                int needed = requiredPoints - request.DonationAmount;
                await mediator.Send(new MooldangBot.Modules.Point.Requests.Commands.AddPointsCommand(
                    request.StreamerUid,
                    request.SenderId,
                    request.Username,
                    needed,
                    MooldangBot.Modules.Point.Enums.PointCurrencyType.DonationPoint,
                    "곡 신청 등록 실패로 인한 환불"
                ), ct);
            }
            return Result<string>.Failure("데이터베이스 저장 중 오류가 발생했습니다. 차감된 포인트는 환불되었습니다.");
        }
    }

    private async Task<int?> ResolveGlobalViewerIdAsync(string senderId, string nickname, CancellationToken ct)
    {
        try 
        {
            return await identityCache.SyncGlobalViewerIdAsync(senderId, nickname, null, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "⚠️ [SongRequest] Failed to resolve GlobalViewerId for {User}", nickname);
            return null;
        }
    }
}
