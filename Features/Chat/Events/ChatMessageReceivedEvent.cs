using MediatR;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.DTOs;

namespace MooldangAPI.Features.Chat.Events;

public record ChatMessageReceivedEvent(
    StreamerProfile Profile,
    string Username,
    string Message,
    string UserRole,
    string SenderId,
    Dictionary<string, string>? Emojis = null,
    int DonationAmount = 0
) : INotification;
