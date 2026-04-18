using MooldangBot.Domain.Contracts.Chzzk.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Domain.Abstractions;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Admin.Handlers;

/// <summary>
/// [관리자의 전령]: 방 관리자용 긴급 설정 핸들러 (레거시 보존용)
/// </summary>
public class ChannelSettingEventHandler_Legacy : INotificationHandler<ChatMessageReceivedEvent_Legacy>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChannelSettingEventHandler_Legacy> _logger;
    private readonly IChzzkBotService _botService;

    // [카테고리 별칭]: 사용자가 입력하는 단축어 -> 검색용 키워드
    private static readonly Dictionary<string, string> CategorySearchAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        { "톡", "talk" },
        { "소통", "talk" },
        { "라디오", "talk" },
        { "먹방", "먹방/쿡방" },
        { "노래", "음악/노래" },
        { "종겜", "종합 게임" },
        { "롤", "League of Legends" },
        { "발로", "발로란트" },
        { "배그", "BATTLEGROUNDS" },
        { "마크", "Minecraft" },
        { "메플", "메이플스토리" },
        { "로아", "로스트아크" },
        { "철권", "철권 8" }
    };

    public ChannelSettingEventHandler_Legacy(ILogger<ChannelSettingEventHandler_Legacy> logger, IServiceProvider serviceProvider, IChzzkBotService botService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _botService = botService;
    }

    public async Task Handle(ChatMessageReceivedEvent_Legacy notification, CancellationToken cancellationToken)
    {
        // [v4.1.0] 방제 및 카테고리 변경 로직은 이제 UnifiedCommandHandler와 각 Strategy(Title, Category)에서 담당합니다.
        // 이 핸들러는 향후 관리자 전용(Admin) 긴급 제어 기능 등을 위해 남겨둡니다.
        
        await Task.CompletedTask;
    }
}
