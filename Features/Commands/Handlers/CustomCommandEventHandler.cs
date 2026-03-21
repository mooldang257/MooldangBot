using MediatR;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Features.Chat.Events;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.ApiClients;

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

            // ⭐ 최신 설정 반영을 위해 DB에서 스트리머 프로필을 즉시 조회합니다.
            var streamerProfile = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == notification.Profile.ChzzkUid, cancellationToken);
            if (streamerProfile == null) return;

            // 포인트 조회 전용 명령어 확인 (`PointCheckCommand`)
            if (!string.IsNullOrWhiteSpace(streamerProfile.PointCheckCommand))
            {
                var pointCmds = streamerProfile.PointCheckCommand.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim().ToLower());
                if (pointCmds.Contains(fullMessage.ToLower()) || pointCmds.Contains(firstWord.ToLower()))
                {
                    string replyText = streamerProfile.PointCheckReply ?? "🪙 {닉네임}님의 보유 포인트는 {포인트}점입니다!";
                replyText = replyText.Replace("{닉네임}", nickname);

                if (replyText.Contains("{출석일수}") || replyText.Contains("{포인트}") || replyText.Contains("{연속출석일수}") || replyText.Contains("{팔로우일수}"))
                {
                    var viewer = await db.ViewerProfiles.FirstOrDefaultAsync(v => v.StreamerChzzkUid == notification.Profile.ChzzkUid && v.ViewerUid == notification.SenderId, cancellationToken);
                    int attendance = viewer?.AttendanceCount ?? 0;
                    int consecutive = viewer?.ConsecutiveAttendanceCount ?? 0;
                    int points = viewer?.Points ?? 0;
                    
                    replyText = replyText.Replace("{출석일수}", attendance.ToString())
                                         .Replace("{연속출석일수}", consecutive.ToString())
                                         .Replace("{포인트}", points.ToString());

                    if (replyText.Contains("{팔로우일수}"))
                    {
                        var apiClient = _serviceProvider.GetRequiredService<ChzzkApiClient>();
                        string? followDateStr = await apiClient.GetViewerFollowDateAsync(
                            notification.Profile.ChzzkAccessToken ?? "",
                            notification.ClientId,
                            notification.ClientSecret,
                            notification.SenderId);

                        if (followDateStr != null && DateTime.TryParse(followDateStr, out var followDate))
                        {
                            int days = (int)(DateTime.UtcNow - followDate.ToUniversalTime()).TotalDays;
                            replyText = replyText.Replace("{팔로우일수}", days.ToString());
                        }
                        else
                        {
                            replyText = replyText.Replace("{팔로우일수}", "?");
                        }
                    }
                }

                string finalReplyText = "\u200B" + replyText;
                if (finalReplyText.Length > 500) finalReplyText = finalReplyText.Substring(0, 497) + "...";
                await ExecuteActionAsync(notification, finalReplyText, "https://openapi.chzzk.naver.com/open/v1/chats/send", cancellationToken);
                return; // 포인트 전용 명령어 처리 후 종료
                }
            }

            // 3. 일반 커스텀 명령어 실행 확인 로직
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
                        string replyText = customCmd.Content.Replace("{닉네임}", nickname);

                        // [추가] 동적 변수 치환 로직
                        if (replyText.Contains("{출석일수}") || replyText.Contains("{포인트}") || replyText.Contains("{연속출석일수}") || replyText.Contains("{팔로우일수}"))
                        {
                            var viewer = await db.ViewerProfiles.FirstOrDefaultAsync(v => v.StreamerChzzkUid == notification.Profile.ChzzkUid && v.ViewerUid == notification.SenderId, cancellationToken);
                            int attendance = viewer?.AttendanceCount ?? 0;
                            int consecutive = viewer?.ConsecutiveAttendanceCount ?? 0;
                            int points = viewer?.Points ?? 0;
                            
                            replyText = replyText.Replace("{출석일수}", attendance.ToString())
                                                 .Replace("{연속출석일수}", consecutive.ToString())
                                                 .Replace("{포인트}", points.ToString());

                            if (replyText.Contains("{팔로우일수}"))
                            {
                                var apiClient = _serviceProvider.GetRequiredService<ChzzkApiClient>();
                                string? followDateStr = await apiClient.GetViewerFollowDateAsync(
                                    notification.Profile.ChzzkAccessToken ?? "",
                                    notification.ClientId,
                                    notification.ClientSecret,
                                    notification.SenderId);

                                if (followDateStr != null && DateTime.TryParse(followDateStr, out var followDate))
                                {
                                    int days = (int)(DateTime.UtcNow - followDate.ToUniversalTime()).TotalDays;
                                    replyText = replyText.Replace("{팔로우일수}", days.ToString());
                                }
                                else
                                {
                                    replyText = replyText.Replace("{팔로우일수}", "?");
                                }
                            }
                        }

                        string finalReplyText = "\u200B" + replyText;
                        if (finalReplyText.Length > 500) finalReplyText = finalReplyText.Substring(0, 497) + "...";
                        await ExecuteActionAsync(notification, finalReplyText, "https://openapi.chzzk.naver.com/open/v1/chats/send", cancellationToken);
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
