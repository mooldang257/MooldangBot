using MediatR;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Features.Chat.Events;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace MooldangAPI.Features.Commands.Handlers;

public class CustomCommandEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomCommandEventHandler> _logger;

    public CustomCommandEventHandler(IServiceProvider serviceProvider, ILogger<CustomCommandEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        string msg = notification.Message;
        string nickname = notification.Username;
        bool isMaster = notification.SenderId == "ca98875d5e0edf02776047fbc70f5449";
        bool isBot = notification.SenderId == "445df9c493713244a65d97e4fd1ed0b1";
        bool isAuthorizedAdmin = isMaster || notification.UserRole == "streamer" || notification.UserRole == "manager";

        // 1. 동적 명령어 등록 (!명령어등록 공지 !트게더 주소)
        if (msg.StartsWith("!명령어등록 ") && isAuthorizedAdmin)
        {
            var parts = msg.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4)
            {
                string actionType = parts[1] == "공지" ? "Notice" : "Reply";
                string triggerWord = parts[2];
                string contentText = parts[3];

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var existingCmd = await db.StreamerCommands.FirstOrDefaultAsync(c => c.ChzzkUid == notification.Profile.ChzzkUid && c.CommandKeyword == triggerWord, cancellationToken);
                if (existingCmd == null)
                {
                    db.StreamerCommands.Add(new StreamerCommand
                    {
                        ChzzkUid = notification.Profile.ChzzkUid,
                        CommandKeyword = triggerWord,
                        ActionType = actionType,
                        Content = contentText,
                        RequiredRole = "manager"
                    });
                }
                else
                {
                    existingCmd.ActionType = actionType;
                    existingCmd.Content = contentText;
                }
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"⚙️ [명령어 등록 완료] {triggerWord} -> {actionType} ({contentText})");
            }
            return;
        }

        // 2. 명령어 실행 확인 로직
        string firstWord = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        string fullMessage = msg.Trim();

        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customCmd = await db.StreamerCommands
                .FirstOrDefaultAsync(c => c.ChzzkUid == notification.Profile.ChzzkUid &&
                                         (c.CommandKeyword == fullMessage || c.CommandKeyword == firstWord), cancellationToken);

            if (customCmd != null)
            {
                bool isAuthorized = isMaster;
                if (!isAuthorized)
                {
                    isAuthorized = true;
                    if (customCmd.RequiredRole == "streamer" && notification.UserRole != "streamer") isAuthorized = false;
                    if (customCmd.RequiredRole == "manager" && !(notification.UserRole == "streamer" || notification.UserRole == "manager")) isAuthorized = false;
                }

                if (isAuthorized)
                {
                    if (customCmd.ActionType == "Notice")
                    {
                        string noticeText = customCmd.Content.Length > 100 ? customCmd.Content.Substring(0, 97) + "..." : customCmd.Content;
                        await ExecuteActionAsync(notification, noticeText, "https://openapi.chzzk.naver.com/open/v1/chats/notice", cancellationToken);
                    }
                    else if (customCmd.ActionType == "Reply" && !isBot)
                    {
                        string replyText = "\u200B" + customCmd.Content;
                        if (replyText.Length > 500) replyText = replyText.Substring(0, 497) + "...";
                        await ExecuteActionAsync(notification, replyText, "https://openapi.chzzk.naver.com/open/v1/chats/send", cancellationToken);
                    }
                }
            }
        }
    }

    private async Task ExecuteActionAsync(ChatMessageReceivedEvent req, string text, string endpoint, CancellationToken token)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Client-Id", req.ClientId);
        client.DefaultRequestHeaders.Add("Client-Secret", req.ClientSecret);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.Profile.ChzzkAccessToken);

        var payload = new { message = text };
        await client.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), token);
    }
}
