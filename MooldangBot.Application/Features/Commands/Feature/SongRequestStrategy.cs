using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Features.Commands.Feature;

/// <summary>
/// [파로스의 선율]: 곡 신청(Song) 명령어를 처리하는 전략입니다.
/// </summary>
public class SongRequestStrategy(
    IServiceProvider serviceProvider,
    IChzzkBotService botService,
    IDynamicQueryEngine dynamicEngine,
    ILogger<SongRequestStrategy> logger) : ICommandFeatureStrategy
{
    public string FeatureType => "SongRequest";

    public async Task<CommandExecutionResult> ExecuteAsync(ChatMessageReceivedEvent notification, UnifiedCommand command, CancellationToken ct)
    {
        // 1. [정수 추출]: 명령어 키워드 이후의 텍스트를 신청곡 명칭으로 인식
        string msg = notification.Message.Trim();
        string[] parts = msg.Split(' ', 2);
        if (parts.Length < 2)
        {
            await botService.SendReplyChatAsync(notification.Profile, "신청곡 제목을 함께 입력해 주세요! (예: !신청 제목) 🎵", notification.SenderId, ct);
            return CommandExecutionResult.Failure("신청곡 제목 누락", shouldRefund: true);
        }

        string songTitle = parts[1];
        
        // 2. [신성한 선율]: 곡 신청 프로세스 진행
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            // 2.1 송리스트 세션 활성화 여부 확인
            var activeSession = await db.SonglistSessions
                .FirstOrDefaultAsync(s => s.ChzzkUid == notification.Profile.ChzzkUid && s.IsActive, ct);

            if (activeSession == null)
            {
                await botService.SendReplyChatAsync(notification.Profile, "현재 송리스트가 비활성화 상태입니다. 🔒", notification.SenderId, ct);
                return CommandExecutionResult.Failure("송리스트 비활성화 상태", shouldRefund: true);
            }

            // 2.2 곡 신청 큐에 저장
            var song = new SongQueue
            {
                ChzzkUid = notification.Profile.ChzzkUid,
                Title = songTitle,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddHours(9)
            };
            db.SongQueues.Add(song);
            await db.SaveChangesAsync(ct);

            logger.LogInformation($"🎵 [곡 신청 완료] {notification.Username}: {songTitle}");

            // 3. [조율된 응답]: 엔진을 통한 동적 변수 치환
            string responseTemplate = string.IsNullOrEmpty(command.ResponseText)
                ? "{닉네임}님의 '{곡제목}' 신청이 완료되었습니다! 🎵"
                : command.ResponseText;

            // {곡제목}은 수동 치환 후 나머지는 엔진에게 위임
            string processedReply = await dynamicEngine.ProcessMessageAsync(
                responseTemplate.Replace("{곡제목}", songTitle, StringComparison.OrdinalIgnoreCase),
                notification.Profile.ChzzkUid,
                notification.SenderId,
                notification.Username
            );

            await botService.SendReplyChatAsync(notification.Profile, processedReply, notification.SenderId, ct);
            return CommandExecutionResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"🔥 [SongRequestStrategy] 오류: {ex.Message}");
            await botService.SendReplyChatAsync(notification.Profile, "⚠️ 곡 신청 처리 중 서버 오류가 발생했습니다. 🌪️", notification.SenderId, ct);
            return CommandExecutionResult.Failure("곡 신청 서버 오류", shouldRefund: true);
        }
    }
}
