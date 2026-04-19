using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Services;
using MooldangBot.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Modules.Roulette.Features.Commands.CompleteRoulette;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Application.Hubs;

/// <summary>
/// [오시리스의 지혜소]: 서버와 오버레이 간의 실시간 공명 허브입니다.
/// (Aegis of Resonance): 현재 JWT(오버레이)와 Cookie(대시보드) 인증을 모두 지원합니다.
/// </summary>
// [v2.4.0]: JWT 인증 종속성 탈피. 해시 데이터 기반의 가벼운 공명 체계로 전환.
// (Aegis of Resonance): OBS 등 외부 환경에서 인증 없이 주소만으로 접속 가능하도록 설정합니다.
[EnableRateLimiting("overlay-high")]
public class OverlayHub(
    IMediator mediator,
    PulseService pulseService,
    ILogger<OverlayHub> logger, 
    IOverlayState overlayState) : Hub
{
    private IOverlayNotificationService GetNotificationService() 
        => Context.GetHttpContext()?.RequestServices.GetRequiredService<IOverlayNotificationService>() 
           ?? throw new InvalidOperationException("IOverlayNotificationService is not available.");
    /// <summary>
    /// [v1.9.9] 오버레이 애니메이션 완료 후 서버에 결과를 알립니다.
    /// </summary>
    public async Task CompleteRouletteAsync(long spinId)
    {
        await mediator.Send(new CompleteRouletteCommand(spinId));
    }

    // [v2.4.0] OBS 브라우저 소스 클라이언트 연결 (해시 기반 식별 강화)
    public override async Task OnConnectedAsync()
    {
        // [오시리스의 인장]: 
        // 1. 기존 쿠키/JWT 세션에서 StreamerId 추출 시도 (대시보드 미리보기 등)
        var chzzkUid = Context.User?.FindFirst("StreamerId")?.Value;

        // 2. 세션이 없는 경우(OBS) 쿼리 스트링에서 16자리 해시 토큰 추출
        if (string.IsNullOrEmpty(chzzkUid))
        {
            var httpContext = Context.GetHttpContext();
            var shortToken = httpContext?.Request.Query["access_token"].ToString();

            if (!string.IsNullOrEmpty(shortToken) && shortToken.Length == 16)
            {
                using var scope = Context.GetHttpContext()?.RequestServices.CreateScope();
                var db = scope?.ServiceProvider.GetRequiredService<IAppDbContext>();
                
                var streamer = await db!.StreamerProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.OverlayToken == shortToken);

                if (streamer != null)
                {
                    chzzkUid = streamer.ChzzkUid;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(chzzkUid))
        {
            var normalizedUid = chzzkUid.ToLower();
            await Groups.AddToGroupAsync(Context.ConnectionId, normalizedUid);
            await overlayState.IncrementAsync(normalizedUid); // Redis 카운터 증가
            
            logger.LogInformation("[오시리스의 공명] 오버레이 연결 성공 (Identity: {ChzzkUid})", normalizedUid);

            // [물멍]: 연결 즉시 현재 신청곡 상태 전송 (초기 데이터 주입)
            await GetNotificationService().BroadcastSongOverlayUpdateAsync(normalizedUid, Context.ConnectionId);
        }
        else
        {
            logger.LogWarning("[오시리스의 불협화음] 인가되지 않은 오버레이 연결 시도 차단 (ConnectionId: {Id})", Context.ConnectionId);
            Context.Abort();
            return;
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var chzzkUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrWhiteSpace(chzzkUid))
        {
            await overlayState.DecrementAsync(chzzkUid.ToLower()); // Redis 카운터 감소
        }

        logger.LogTrace("[오시리스의 회상] 오버레이 연결 종료. Group: {ChzzkUid}, ConnectionId: {ConnectionId}", chzzkUid ?? "Unknown", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// [v2.2.0] 스트리머 전용 그룹에 명시적으로 합류합니다.
    /// </summary>
    public async Task JoinStreamerGroup()
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, streamerUid.ToLower());
        }
    }

    public async Task LeaveStreamerGroup()
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, streamerUid.ToLower());
        }
    }

    // 상태 업데이트를 스트리머 그룹에 전송
    public async Task UpdateOverlayState(string stateJson)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayState", stateJson);
        }
    }

    // 프리셋 그룹 관리
    public async Task JoinPresetGroup(int presetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"preset-{presetId}");
    }

    public async Task LeavePresetGroup(int presetId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"preset-{presetId}");
    }

    // 스타일 업데이트 브로드캐스트
    public async Task UpdatePresetStyle(int presetId, string styleJson)
    {
        await Clients.Group($"preset-{presetId}").SendAsync("ReceiveOverlayStyle", styleJson);
    }

    public async Task UpdateOverlayStyle(string styleJson)
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            await Clients.Group(streamerUid.ToLower()).SendAsync("ReceiveOverlayStyle", styleJson);
        }
    }

    /// <summary>
    /// [v2.2.1] 클라이언트의 생존 신호를 수신합니다.
    /// </summary>
    public async Task ReportPulse()
    {
        var streamerUid = Context.User?.FindFirst("StreamerId")?.Value;
        if (!string.IsNullOrEmpty(streamerUid))
        {
            pulseService.ReportPulse($"Overlay:{streamerUid.ToLower()}");
        }
    }
}
