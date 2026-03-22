using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Features.Chat.Events;
using System.Net.Http.Headers;

namespace MooldangAPI.Features.Viewers.Handlers;

public class ViewerPointEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ViewerPointEventHandler> _logger;

    public ViewerPointEventHandler(IServiceProvider serviceProvider, ILogger<ViewerPointEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var streamerUid = notification.Profile.ChzzkUid;
        var viewerUid = notification.SenderId;
        var nickname = notification.Username;

        if (string.IsNullOrEmpty(streamerUid) || string.IsNullOrEmpty(viewerUid)) return;

        var viewer = await db.ViewerProfiles
            .FirstOrDefaultAsync(v => v.StreamerChzzkUid == streamerUid && v.ViewerUid == viewerUid, cancellationToken);

        if (viewer == null)
        {
            viewer = new ViewerProfile
            {
                StreamerChzzkUid = streamerUid,
                ViewerUid = viewerUid,
                Nickname = nickname,
                Points = 0,
                AttendanceCount = 0
            };
            db.ViewerProfiles.Add(viewer);
        }
        else
        {
            if (viewer.Nickname != nickname)
                viewer.Nickname = nickname;
        }

        var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == streamerUid, cancellationToken);
        if (streamer == null) return;

        int pointToAdd = streamer.PointPerChat;
        bool isAttendance = false;

        // 출석 명령어 체크
        if (!string.IsNullOrWhiteSpace(streamer.AttendanceCommands))
        {
            var attCmds = streamer.AttendanceCommands.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(c => c.Trim().ToLower())
                                                     .ToList();

            string msgLower = notification.Message.Trim().ToLower();

            if (attCmds.Contains(msgLower))
            {
                var koreaTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Korea Standard Time");
                
                // DB에 KST로 저장되어 있으므로 그대로 날짜만 비교합니다.
                if (!viewer.LastAttendanceAt.HasValue || viewer.LastAttendanceAt.Value.Date < koreaTime.Date)
                {
                    var prevLastAtt = viewer.LastAttendanceAt;

                    // 연속 출석일 계산
                    if (prevLastAtt.HasValue && prevLastAtt.Value.Date == koreaTime.Date.AddDays(-1))
                    {
                        viewer.ConsecutiveAttendanceCount++;
                    }
                    else
                    {
                        viewer.ConsecutiveAttendanceCount = 1;
                    }

                    viewer.AttendanceCount++;
                    viewer.LastAttendanceAt = koreaTime; // DB에 한국 시간(KST)으로 저장
                    pointToAdd += streamer.PointPerAttendance;
                    isAttendance = true;
                    _logger.LogInformation($"📅 [출석 인정] {nickname}님 출석! (누적 {viewer.AttendanceCount}회, 연속 {viewer.ConsecutiveAttendanceCount}일)");

                    // 출석 챗 응답 발송 로직
                    if (!string.IsNullOrWhiteSpace(streamer.AttendanceReply))
                    {
                        string replyMsg = streamer.AttendanceReply
                            .Replace("{닉네임}", nickname)
                            .Replace("{연속출석일수}", viewer.ConsecutiveAttendanceCount.ToString())
                            .Replace("{누적출석일수}", viewer.AttendanceCount.ToString());
                        
                        if (replyMsg.Contains("{마지막출석일}"))
                        {
                            string lastDateString = prevLastAtt.HasValue ? prevLastAtt.Value.ToString("yyyy.MM.dd") : "없음";
                            replyMsg = replyMsg.Replace("{마지막출석일}", lastDateString);
                        }

                        // 백그라운드에서 발송
                        if (!string.IsNullOrEmpty(notification.Profile.ChzzkAccessToken))
                        {
                            _ = SendChatReplyAsync(notification.Profile.ChzzkAccessToken, notification.ClientId, notification.ClientSecret, replyMsg);
                        }
                    }
                }
            }
        }

        // 후원 금액 비례 포인트 계산
        if (notification.DonationAmount > 0)
        {
            int donationPoints = (notification.DonationAmount / 1000) * streamer.PointPerDonation1000;
            pointToAdd += donationPoints;
        }

        viewer.Points += pointToAdd;
        await db.SaveChangesAsync(cancellationToken);

        if (pointToAdd > 0)
        {
            _logger.LogDebug($"[포인트 적립] {nickname}: +{pointToAdd}점 (현재 {viewer.Points}점, 출석:{isAttendance})");
        }
    }

    private async Task SendChatReplyAsync(string accessToken, string clientId, string clientSecret, string message)
    {
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret)) return;
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-Id", clientId);
            client.DefaultRequestHeaders.Add("Client-Secret", clientSecret);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var reqBody = new { message = "\u200B" + message };
            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(reqBody);
            await client.PostAsync("https://openapi.chzzk.naver.com/open/v1/chats/send", 
                new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json"));
        }
        catch (Exception ex)
        {
            _logger.LogError($"출석 응답 발송 실패: {ex.Message}");
        }
    }
}
