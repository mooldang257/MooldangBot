using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Domain.Entities;
using MooldangBot.Domain.Common;
using MooldangBot.ChzzkAPI.Interfaces;

namespace MooldangBot.Application.Services;

    private readonly IRabbitMqService _rabbitMq;
    private readonly IDynamicQueryEngine _dynamicEngine; 
    private readonly ILogger<ChzzkBotService> _logger;

    public ChzzkBotService(
        IRabbitMqService rabbitMq, 
        IDynamicQueryEngine dynamicEngine, 
        ILogger<ChzzkBotService> logger)
    {
        _rabbitMq = rabbitMq;
        _dynamicEngine = dynamicEngine;
        _logger = logger;
    }


    public async Task<bool> SendReplyChatAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token)
    {
        return await SendGenericChatAsync(profile, message, viewerUid, false, token);
    }

    public async Task<bool> SendReplyNoticeAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token)
    {
        return await SendGenericChatAsync(profile, message, viewerUid, true, token);
    }

    public async Task<string?> GetStreamerTokenAsync(StreamerProfile profile)
    {
        // [v13.1] 통합 갱신 엔진(ITokenRenewalService)으로 일원화
        await _renewalService.RenewIfNeededAsync(profile.ChzzkUid);
        
        // 갱신 후 최신 상태의 토큰 반환 (RenewIfNeeded 내부에서 SaveChanges 수행함)
        return profile.ChzzkAccessToken;
    }

    private async Task<bool> SendGenericChatAsync(StreamerProfile profile, string message, string viewerUid, bool isNotice, CancellationToken token)
    {
        try
        {
            await Task.Delay(100, token);
            string processedMessage = await _dynamicEngine.ProcessMessageAsync(message, profile.ChzzkUid, viewerUid);

            _logger.LogInformation($"📡 [봇 명령 발행] 대상채널: {profile.ChzzkUid}, 타입: {(isNotice ? "상단공지" : "일반")}");

            // [v2.2] 원격 명령 하달: 직접 소켓을 호출하지 않고 RabbitMQ로 명령 발행
            var command = new ChzzkBotCommand(
                Guid.NewGuid(), 
                profile.ChzzkUid, 
                BotCommandType.SendMessage, 
                processedMessage, 
                isNotice);

            await _rabbitMq.PublishAsync(command, "", RabbitMqExchanges.BotCommands);

            return true; // 발행 성공 시 일단 true 반환 (비동기 처리)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [ChzzkBotService] {profile.ChzzkUid} 명령 발행 에러: {ex.Message}");
            return false;
        }
    }

    public async Task RefreshChannelAsync(string chzzkUid)
    {
        _logger.LogInformation($"🔄 [피닉스의 전령] {chzzkUid} 채널의 설정을 새로고침하고 연결을 점검합니다.");
        await EnsureConnectionAsync(chzzkUid);
    }

    public async Task EnsureConnectionAsync(string chzzkUid, bool forceFresh = false)
    {
        // [v2.3] 원격 제어: Api 서버는 상태를 직접 관리하지 않고 봇 엔진에 재연결 명령만 전달합니다.
        _logger.LogInformation($"🔄 [인드라의 명령] {chzzkUid} 채널에 대한 재연결 명령을 발행합니다.");
        
        var command = new ChzzkBotCommand(
            Guid.NewGuid(), 
            chzzkUid, 
            BotCommandType.Reconnect, 
            null);

        await _rabbitMq.PublishAsync(command, "", RabbitMqExchanges.BotCommands);
    }

    public async Task HandleAuthFailureAsync(string chzzkUid)
    {
        // [v2.3] 자가 치유는 이제 봇 엔진(ChzzkAPI)의 책임입니다. 
        // Api 서버는 갱신 명령만 전달합니다.
        _logger.LogWarning($"🚨 [자가 치유 요청] {chzzkUid} 채널에 대한 토큰 새로고침 명령을 발행합니다.");
        
        var command = new ChzzkBotCommand(
            Guid.NewGuid(), 
            chzzkUid, 
            BotCommandType.RefreshSettings, 
            null);

        await _rabbitMq.PublishAsync(command, "", RabbitMqExchanges.BotCommands);
    }

    public void CleanupRecoveryLock(string chzzkUid) { /* No-op in distributed mode */ }

}
