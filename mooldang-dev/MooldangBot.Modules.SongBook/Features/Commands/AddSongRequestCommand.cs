using MooldangBot.Modules.SongBook.Events;
using MooldangBot.Modules.SongBook.State;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Contracts.SongBook;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Common.Models;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.Modules.SongBook.Abstractions;
using MooldangBot.Domain.Contracts.AI.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MooldangBot.Domain.Contracts.SongBook.Events;

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
    IVectorEmbeddingService embeddingService,
    IVectorSearchRepository vectorRepository,
    ILogger<AddSongRequestCommandHandler> logger) : IRequestHandler<AddSongRequestCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddSongRequestCommand request, CancellationToken ct)
    {
        logger.LogInformation("🚀 [SongRequest] Starting request processing: {Title} from {User} (Donation: {Donation})", request.SongTitle, request.Username, request.DonationAmount);

        // 1. [식별]: 스트리머 및 시청자 정보 로드
        var Profile = await db.TableCoreStreamerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ChzzkUid == request.StreamerUid, ct);
 
        if (Profile == null) return Result<string>.Failure("스트리머 정보를 찾을 수 없습니다.");
        logger.LogInformation("🔍 [SongRequest] Repo Type: {RepoType}", repository.GetType().FullName);
 
        float[]? Vector = null;
        try 
        {
            // [오시리스의 예지]: BGE-M3 로컬 임베딩을 사용하여 다국어 의미론적 공간 확보
            Vector = await embeddingService.GetEmbeddingAsync(request.SongTitle);
        }
        catch (Exception Ex)
        {
            logger.LogWarning(Ex, "⚠️ [SongRequest] Failed to get BGE-M3 embedding.");
        }

        // [v19.0]: 개인 노래책(FuncSongBooks)에서 우선 수색
        var PersonalResults = await repository.SearchPersonalSongBookAsync(Profile.Id, request.SongTitle, Vector, limit: 1);
        var MatchedSong = PersonalResults.FirstOrDefault();
        
        string FinalTitle;
        string? Artist = null;
        string? ThumbnailUrl = null;
        string? Pitch = null;
        int RequiredPoints = request.RequiredPoints;
 
        if (MatchedSong != null)
        {
            FinalTitle = MatchedSong.Title;
            Artist = MatchedSong.Artist;
            ThumbnailUrl = MatchedSong.ThumbnailUrl;
            Pitch = MatchedSong.Pitch;
            RequiredPoints = MatchedSong.RequiredPoints;
            logger.LogInformation("🎯 [SongRequest] Match Found in FuncSongBooks: {Title} (Cost: {Points} Cheese)", FinalTitle, RequiredPoints);
        }
        else
        {
            // [오시리스의 영속]: 개인 노래책에 없으면 글로벌 메타데이터에서 하이브리드 수색 (자동 교정)
            if (Vector != null)
            {
                var GlobalMatches = await vectorRepository.SearchHybridAsync<GlobalMusicMetadata>(
                    "GlobalMusicMetadata", request.SongTitle, Vector, limit: 1);
                
                var GlobalSong = GlobalMatches.FirstOrDefault();
                if (GlobalSong != null)
                {
                    FinalTitle = GlobalSong.NormalizedTitle;
                    Artist = GlobalSong.NormalizedArtist;
                    ThumbnailUrl = GlobalSong.ThumbnailUrl;
                    logger.LogInformation("🧠 [SongRequest] Semantic Correction Applied: '{Input}' -> '{Corrected}' by {Artist}", request.SongTitle, FinalTitle, Artist);
                    goto Processing; // 교정 완료 시 AI 추출 스킵
                }
            }

            logger.LogInformation("❓ [SongRequest] No match found. Attempting AI Title Extraction for '{Input}'", request.SongTitle);
            try
            {
                var Prompt = $@"Extract the song title and artist from the following request: ""{request.SongTitle}""
                Return as JSON: {{ ""title"": ""..."", ""artist"": ""..."" }}
                If you can't find it, use the original text as title.";
                var Extracted = await llmService.GenerateResponseAsync(Prompt, "");
                logger.LogInformation("🤖 [SongRequest] AI Raw Result: {Result}", Extracted);
 
                using var Doc = JsonDocument.Parse(Extracted);
                FinalTitle = Doc.RootElement.GetProperty("title").GetString() ?? request.SongTitle;
                Artist = Doc.RootElement.TryGetProperty("artist", out var a) ? a.GetString() : null;
            }
            catch (Exception Ex)
            {
                logger.LogWarning("⚠️ [SongRequest] AI Extraction failed: {Msg}. Using raw title.", Ex.Message);
                FinalTitle = request.SongTitle;
            }
        }
 
        Processing: // 레이블 추가
        logger.LogInformation("📝 [SongRequest] Final Title for Queue: {Title}", FinalTitle);

        // 2. [결제]: 포인트 차감 처리
        if (request.DonationAmount < RequiredPoints)
        {
            int Needed = RequiredPoints - request.DonationAmount;
            logger.LogInformation("💳 [SongRequest] Points deduction: {Needed}", Needed);
            
            var DeductResult = await mediator.Send(new MooldangBot.Modules.Point.Requests.Commands.DeductDonationPointsCommand(
                request.StreamerUid,
                request.SenderId,
                Needed
            ), ct);
 
            if (!DeductResult.Success)
            {
                logger.LogWarning("⚠️ [SongRequest] Insufficient balance for {User}: Needs {Needed}", request.Username, Needed);
                return Result<string>.Failure($"포인트가 부족합니다. (현재 잔액: {DeductResult.CurrentBalance} / 필요: {Needed} 치즈 🧀)");
            }
        }

        // 3. [영속화]: DB에 신청 내역 저장
        try 
        {
            var QueueCount = await db.TableFuncSongListQueues
                .Where(q => q.StreamerProfileId == Profile.Id && !q.IsDeleted)
                .CountAsync(ct);
 
            var NewRequest = new FuncSongListQueues
            {
                StreamerProfileId = Profile.Id,
                GlobalViewerId = await ResolveGlobalViewerIdAsync(request.SenderId, request.Username, ct),
                RequesterNickname = request.Username,
                Title = FinalTitle,
                Artist = Artist,
                Status = SongStatus.Pending,
                CreatedAt = KstClock.Now,
                SortOrder = QueueCount + 1,
                VideoId = MatchedSong?.ReferenceUrl, 
                ThumbnailUrl = ThumbnailUrl,
                Pitch = Pitch
            };
 
            db.TableFuncSongListQueues.Add(NewRequest);
            await db.SaveChangesAsync(ct);
            int NewSongId = NewRequest.Id;
            logger.LogInformation("✅ [SongRequest] Successfully saved to DB (ID: {Id})", NewSongId);
 
            // 인메모리 버퍼 실제 추가
            var Added = state.AddSong(request.StreamerUid, NewSongId, request.Username, FinalTitle, Artist, MatchedSong?.ReferenceUrl, ThumbnailUrl, Pitch);
            if (!Added) logger.LogWarning("⚠️ [SongRequest] Failed to add to in-memory state.");
            
            // 4. [알림]: 오버레이 업데이트 통지
            await overlayNotification.BroadcastSongOverlayUpdateAsync(request.StreamerUid, null, ct);
            await overlayNotification.NotifyPointChangedAsync(request.StreamerUid, ct);

            // [오시리스의 예지]: 썸네일이 없거나 벡터가 없는 경우 비동기 수집 요청
            if (string.IsNullOrEmpty(ThumbnailUrl))
            {
                await mediator.Publish(new SongMetadataFetchEvent(Artist ?? "", FinalTitle), ct);
            }

            logger.LogInformation("📡 [SongRequest] All notifications (Overlay, Queue, Dashboard) sent.");
 
            return Result<string>.Success(FinalTitle);
        }
        catch (Exception Ex)
        {
            logger.LogError(Ex, "❌ [SongRequest] Persistence failed. (Refund process starting)");
            // 환불 로직
            if (request.DonationAmount < RequiredPoints)
            {
                int Needed = RequiredPoints - request.DonationAmount;
                await mediator.Send(new MooldangBot.Modules.Point.Requests.Commands.AddPointsCommand(
                    request.StreamerUid,
                    request.SenderId,
                    request.Username,
                    Needed,
                    MooldangBot.Modules.Point.Enums.PointCurrencyType.DonationPoint,
                    "곡 신청 등록 실패로 인한 환불"
                ), ct);
            }
            return Result<string>.Failure("데이터베이스 저장 중 오류가 발생했습니다. 차감된 포인트는 환불되었습니다.");
        }
    }

    private async Task<int?> ResolveGlobalViewerIdAsync(string SenderId, string Nickname, CancellationToken ct)
    {
        try 
        {
            return await identityCache.SyncGlobalViewerIdAsync(SenderId, Nickname, null, ct);
        }
        catch (Exception Ex)
        {
            logger.LogError(Ex, "⚠️ [SongRequest] Failed to resolve GlobalViewerId for {User}", Nickname);
            return null;
        }
    }
}
