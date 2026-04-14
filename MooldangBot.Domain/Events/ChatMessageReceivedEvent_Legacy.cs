using MediatR;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangBot.Domain.Events;

public record ChatMessageReceivedEvent_Legacy(
    Guid CorrelationId,    // [v2.2] 추적성 강화를 위한 상관관계 ID
    StreamerProfile Profile,
    string Username,
    string Message,
    string UserRole,
    string SenderId,
    System.Text.Json.JsonElement? Emojis = null,
    int DonationAmount = 0,
    string? SafeEventId = null // [v7.1] 멱등성 검증 통과 식별자
) : INotification;
