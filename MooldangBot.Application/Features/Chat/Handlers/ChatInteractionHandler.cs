using MooldangBot.Contracts.Events;
using MediatR;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Contracts.Events;
using MooldangBot.Contracts.Integrations.Chzzk.Models.Events;
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
    ICommandCacheService commandCache,
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
            // 봇 자신의 말은 이미 중앙(Consumer)에서 걸러졌으므로 여기선 별도 처리 불필요

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

        // 2. [명령어 판별 로직]: DB 키워드 대조 (키워드 + ' ' 또는 키워드 단독)
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
        // 첫 단어 추출
        var parts = message.Split(' ', 2);
        string firstWord = parts[0];

        // DB 키워드 캐시 확인
        var command = await commandCache.GetUnifiedCommandAsync(chzzkUid, firstWord);

        // 결과 반환: 키워드가 존재하면 명령어로 간주
        return command != null;
    }
}
