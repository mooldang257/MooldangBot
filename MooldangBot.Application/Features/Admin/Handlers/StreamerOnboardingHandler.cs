using MediatR;
using Microsoft.Extensions.Logging;
using MooldangBot.Modules.Commands.Abstractions;
using MooldangBot.Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Admin.Handlers;

/// <summary>
/// [오시리스의 온보딩]: 신규 스트리머 등록 이벤트를 포착하여 기본 명령어 및 룰렛 시딩을 비동기로 집행합니다.
/// </summary>
public class StreamerOnboardingHandler : INotificationHandler<StreamerRegisteredEvent>
{
    private readonly IUnifiedCommandService _commandService;
    private readonly ILogger<StreamerOnboardingHandler> _logger;

    public StreamerOnboardingHandler(IUnifiedCommandService commandService, ILogger<StreamerOnboardingHandler> logger)
    {
        _commandService = commandService;
        _logger = logger;
    }

    public async Task Handle(StreamerRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🌱 [Onboarding]: '신규 스트리머 등록({ChannelName})' 이벤트를 수신했습니다. 명령어 시딩을 시작합니다.", notification.ChannelName);

        try
        {
            // [지휘관님의 지침]: 인증 프로세스에 영향을 주지 않도록 내부에서 예외를 완벽히 격리합니다.
            // 기본 명령어(!공지, !신청, !룰렛 등)와 그에 딸린 룰렛 데이터가 생성됩니다.
            await _commandService.InitializeDefaultCommandsAsync(notification.ChzzkUid);
            
            _logger.LogInformation("✅ [Onboarding]: 스트리머({ChannelName})에 대한 기본 온보딩 작업이 성공적으로 완료되었습니다.", notification.ChannelName);
        }
        catch (Exception ex)
        {
            // [오시리스의 비호]: 온보딩 실패가 로그인 자체를 막아서는 안 됩니다 (Fault Tolerance).
            _logger.LogError(ex, "❌ [Onboarding]: 스트리머({ChannelName}) 기본 명령어 시딩 중 오류 발생. (로그 후 인증 성공 유지)", notification.ChannelName);
        }
    }
}
