using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Modules.Commands.Events;
using MediatR;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Events;
using MooldangBot.Domain.Contracts.Chzzk.Models.Events;
using Microsoft.Extensions.Logging;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Chat.Handlers;

/// <summary>
/// [서기의 기록]: 모든 채팅 상호작용을 감사(Audit)하고 기록하며, 명령어 여부를 판별하는 핸들러입니다.
/// </summary>
public class ChatInteractionHandler(
    IChatLogBufferService bufferService,
    ICommandCache commandCache,
    IBroadcastScribe scribe, // [v3.7] 기존 통계 엔진 유지
    ILogger<ChatInteractionHandler> logger) : INotificationHandler<ChzzkEventReceived>
{
    public async Task Handle(ChzzkEventReceived notification, CancellationToken ct)
    {
        // 1. [다형성 추출]: 채팅 또는 후원 이벤트 처리
        if (notification.Payload is not (ChzzkChatEvent or ChzzkDonationEvent))
            return;

        var profile = notification.Profile;
        if (string.IsNullOrEmpty(profile.ChzzkUid)) return;

        string senderNickname = string.Empty;
        string message = string.Empty;
        string messageType = "Chat";

        if (notification.Payload is ChzzkChatEvent chat)
        {
            senderNickname = chat.Nickname;
            message = chat.Content;
            messageType = "Chat";
        }
        else if (notification.Payload is ChzzkDonationEvent donation)
        {
            senderNickname = donation.Nickname;
            message = donation.DonationMessage;
            messageType = "Donation";
        }

        if (string.IsNullOrEmpty(message)) return;

        // 2. [명령어 판별 로직]: 신규 매칭 엔진(ICommandCache) 위임
        bool isCommand = await DetermineIfCommandAsync(profile.ChzzkUid, message);

        // 3. [영속성 위임]: 버퍼 서비스를 통해 벌크 적재 요청 (Non-blocking)
        var log = new ChatInteractionLog
        {
            StreamerProfileId = profile.Id,
            SenderNickname = senderNickname,
            Message = message,
            IsCommand = isCommand,
            MessageType = messageType,
            CreatedAt = KstClock.Now
        };

        bufferService.Enqueue(log);

        // 4. [기존 통계 연동]: BroadcastScribe에도 전달 (기존Keywords/Emotes 수집용)
        scribe.AddChatMessage(profile.ChzzkUid, message);

        if (isCommand)
            logger.LogDebug("📝 [Interaction Log] Command Detected: {Message} by {User}", message, senderNickname);
    }

    private async Task<bool> DetermineIfCommandAsync(string chzzkUid, string message)
    {
        // 신규 매칭 엔진을 통해 해당 메시지가 어떤 명령어라도 트리거하는지 확인 (Exact, Prefix, Contains, Regex)
        var matches = await commandCache.GetMatchesAsync(chzzkUid, message);
        return matches.Any();
    }
}
