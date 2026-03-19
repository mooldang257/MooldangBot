using MediatR;
using MooldangAPI.Models;

namespace MooldangAPI.Features.Chat.Events;

public record ChatMessageReceivedEvent(
    StreamerProfile Profile,
    string Username,
    string Message,
    string UserRole,
    string SenderId,
    string ClientId,
    string ClientSecret,
    Dictionary<string, string>? Emojis = null
) : INotification;
