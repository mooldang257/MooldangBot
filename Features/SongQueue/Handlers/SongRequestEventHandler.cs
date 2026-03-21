using MediatR;
using MooldangAPI.Data;
using MooldangAPI.Models;
using MooldangAPI.Features.Chat.Events;
using Microsoft.AspNetCore.SignalR;
using MooldangAPI.Hubs;

namespace MooldangAPI.Features.SongQueue.Handlers;

public class SongRequestEventHandler : INotificationHandler<ChatMessageReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SongRequestEventHandler> _logger;

    public SongRequestEventHandler(IServiceProvider serviceProvider, ILogger<SongRequestEventHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken cancellationToken)
    {
        var profile = notification.Profile;
        string msg = notification.Message;
        string nickname = notification.Username;
        
        string songCmd = profile.SongCommand ?? "!신청";
        
        if (msg.StartsWith(songCmd) && msg.Length > songCmd.Length)
        {
            string songInput = msg.Substring(songCmd.Length).Trim();
            _logger.LogInformation($"🎵 [곡 신청 포착] {nickname}님 -> {songInput}");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                int maxOrder = db.SongQueues
                    .Where(s => s.ChzzkUid == profile.ChzzkUid)
                    .Max(s => (int?)s.SortOrder) ?? 0;

                var newSong = new MooldangAPI.Models.SongQueue
                {
                    ChzzkUid = profile.ChzzkUid,
                    Title = songInput,
                    Artist = nickname,
                    Status = "Pending",
                    SortOrder = maxOrder + 1,
                    CreatedAt = DateTime.Now
                };

                db.SongQueues.Add(newSong);
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation($"✅ [DB 저장 완료] {songInput} (신청자: {nickname}, 순번: {newSong.SortOrder})");

                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<OverlayHub>>();
                await hubContext.Clients.Group(profile.ChzzkUid).SendAsync("RefreshSonglist", cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ [DB 저장 실패] {ex.Message}");
            }
        }
    }
}
