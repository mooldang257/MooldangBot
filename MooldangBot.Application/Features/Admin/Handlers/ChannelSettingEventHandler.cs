using MooldangBot.Contracts.Chzzk.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MooldangBot.Contracts.Common.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Events;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MooldangBot.Application.Features.Admin.Handlers;

public class ChannelSettingEventHandler : INotificationHandler<ChatMessageReceivedEvent_Legacy>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChannelSettingEventHandler> _logger;
    private readonly IChzzkBotService _botService;

    // 💡 [카테고리 사전]: 사용자가 입력하는 단축어 -> 검색용 키워드
    private static readonly Dictionary<string, string> CategorySearchAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        { "저챗", "talk" },
        { "소통", "talk" },
        { "노가리", "talk" },
        { "먹방", "먹방/쿡방" },
        { "노래", "음악/노래" },
        { "종겜", "종합 게임" },
        { "롤", "리그 오브 레전드" },
        { "발로", "발로란트" },
        { "배그", "BATTLEGROUNDS" },
        { "마크", "Minecraft" },
        { "메", "메이플스토리" },
        { "로아", "로스트아크" },
        { "철권", "철권 8" }
    };

    public ChannelSettingEventHandler(ILogger<ChannelSettingEventHandler> logger, IServiceProvider serviceProvider, IChzzkBotService botService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _botService = botService;
    }
    
    // ...

    public async Task Handle(ChatMessageReceivedEvent_Legacy notification, CancellationToken cancellationToken)
    {
        // [v4.1.0] 방제 및 카테고리 변경 로직은 이제 UnifiedCommandHandler와 각 Strategy(Title, StreamCategory)에서 담당합니다.
        // 이 핸들러는 향후 관리자 전용(Admin) 긴급 제어 기능 등을 위해 남겨둡니다.
        
        /* 💡 기존 로직 제거 내역 (이동 완료)
           - !방제 -> TitleStrategy
           - !카테고리 -> CategoryStrategy (별칭 포함)
        */
        
        await Task.CompletedTask;
    }
}
