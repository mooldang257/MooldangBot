using MediatR;
using Microsoft.EntityFrameworkCore;
using MooldangAPI.ApiClients;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Features.Chat.Events;
using System.Net.Http.Headers;
using MooldangAPI.Services;

namespace MooldangAPI.Features.Viewers.Handlers;

public class ViewerPointEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ViewerPointEventHandler> _logger;
    private readonly ChzzkApiClient _chzzkApi;

    public ViewerPointEventHandler(IServiceProvider serviceProvider, ILogger<ViewerPointEventHandler> logger, ChzzkApiClient chzzkApi)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _chzzkApi = chzzkApi;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var streamerUid = notification.Profile.ChzzkUid;
        var viewerUid = notification.SenderId;
        var nickname = notification.Username;

        // --- 데이터 조회 및 판별 (Tracking 쿼리) ---
        var streamer = await db.StreamerProfiles.FirstOrDefaultAsync(p => p.ChzzkUid == streamerUid, cancellationToken);
        if (streamer == null) return;

        var viewer = await db.ViewerProfiles.FirstOrDefaultAsync(v => v.StreamerChzzkUid == streamerUid && v.ViewerUid == viewerUid, cancellationToken);

        var koreaTime = MooldangAPI.Common.TimeContext.KstNow;
        var today = koreaTime.Date;
        
        int totalPointToAdd = streamer.PointPerChat; // 기본 채팅 포인트
        bool isFirstAttendanceToday = false;

        // 1. 출석 명령어 체크
        if (!string.IsNullOrWhiteSpace(streamer.AttendanceCommands))
        {
            var attCmds = streamer.AttendanceCommands.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(c => c.Trim().ToLower())
                                                     .ToList();

            string userMsg = notification.Message.Trim().ToLower();

            if (attCmds.Contains(userMsg))
            {
                var lastDate = viewer?.LastAttendanceAt?.Date;

                // 오늘 이미 출석했는지 체크 (.Date 비교로 시간 오차 방지)
                if (lastDate != today)
                {
                    isFirstAttendanceToday = true;
                    totalPointToAdd += streamer.PointPerAttendance;

                    // 2. 뷰어 프로필 및 출석 데이터 갱신
                    if (viewer == null)
                    {
                        viewer = new ViewerProfile
                        {
                            StreamerChzzkUid = streamerUid,
                            ViewerUid = viewerUid,
                            Nickname = nickname,
                            Points = 0,
                            AttendanceCount = 1,
                            ConsecutiveAttendanceCount = 1,
                            LastAttendanceAt = koreaTime
                        };
                        db.ViewerProfiles.Add(viewer);
                    }
                    else
                    {
                        // 연속 출석 판별 (어제 출석했는가?)
                        if (lastDate.HasValue && (today - lastDate.Value).TotalDays == 1)
                            viewer.ConsecutiveAttendanceCount++;
                        else
                            viewer.ConsecutiveAttendanceCount = 1;

                        viewer.AttendanceCount++;
                        viewer.LastAttendanceAt = koreaTime;
                        viewer.Nickname = nickname; // 닉네임 동기화
                    }
                }
                else
                {
                    _logger.LogInformation($"[출석 스킵] {nickname}님은 이미 오늘 출석했습니다.");
                }
            }
        }

        // 3. 후원 포인트 계산
        if (notification.DonationAmount > 0)
        {
            int donationPoints = (notification.DonationAmount / 1000) * streamer.PointPerDonation1000;
            totalPointToAdd += donationPoints;
        }

        // 4. 포인트 합산 및 DB 최종 반영
        if (viewer != null)
        {
            viewer.Points += totalPointToAdd;
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"✅ [출석/포인트 적립] {nickname}: +{totalPointToAdd}점 (현재 {viewer.Points}점, 오늘첫출석:{isFirstAttendanceToday})");

            // 5. [자동 응답] 오늘 첫 출석인 경우에만 템플릿 치환 후 발송
            if (isFirstAttendanceToday && !string.IsNullOrWhiteSpace(streamer.AttendanceReply))
            {
                string reply = streamer.AttendanceReply
                    .Replace("{닉네임}", nickname)
                    .Replace("{포인트}", viewer.Points.ToString("N0"))
                    .Replace("{출석일수}", viewer.AttendanceCount.ToString())
                    .Replace("{연속출석일수}", viewer.ConsecutiveAttendanceCount.ToString());

                await SendChatReplyAsync(_chzzkApi, streamer.ChzzkAccessToken ?? "", reply);
            }
        }
        else if (totalPointToAdd > 0)
        {
            // 신규 유저가 출석 키워드 없이 첫 채팅만 친 경우 등의 폴백
            var pointService = scope.ServiceProvider.GetRequiredService<IPointTransactionService>();
            await pointService.AddPointsAsync(streamerUid, viewerUid, nickname, totalPointToAdd, cancellationToken);
            _logger.LogInformation($"✅ [포인트 적립] {nickname}: +{totalPointToAdd}점 (신규 방문자)");
        }
    }

    private async Task SendChatReplyAsync(ChzzkApiClient chzzkApi, string accessToken, string message)
    {
        if (string.IsNullOrEmpty(accessToken) || chzzkApi == null) return;
        try
        {
            // [성능 개선 #4] 하드코딩된 new HttpClient()를 제거하고 주입된 ChzzkApiClient를 사용
            bool success = await chzzkApi.SendChatMessageAsync(accessToken, message);
            if (!success)
            {
                _logger.LogWarning($"채팅 응답 발송 실패 (상태 코드 미보장)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"출석 응답 발송 실패: {ex.Message}");
        }
    }
}
