using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Models;
using MooldangBot.Application.Models.Chzzk;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Application.Services;

public class ChzzkBotService : IChzzkBotService
{
    private readonly IRabbitMqService _rabbitMq;
    private readonly IDynamicQueryEngine _dynamicEngine; 
    private readonly ITokenRenewalService _renewalService;
    private readonly ILogger<ChzzkBotService> _logger;

    public ChzzkBotService(
        IRabbitMqService rabbitMq, 
        IDynamicQueryEngine dynamicEngine, 
        ITokenRenewalService renewalService,
        ILogger<ChzzkBotService> logger)
    {
        _rabbitMq = rabbitMq;
        _dynamicEngine = dynamicEngine;
        _renewalService = renewalService;
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
                isNotice ? BotCommandType.SendChatNotice : BotCommandType.SendMessage, 
                processedMessage, 
                KstClock.Now); // [v2.5] 공지 타입 구분 로직 적용

            await _rabbitMq.PublishAsync(command, "", RabbitMqExchanges.BotCommands);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [ChzzkBotService] {profile.ChzzkUid} 명령 발행 에러: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateTitleAsync(StreamerProfile profile, string newTitle, string senderUid, CancellationToken token)
    {
        _logger.LogInformation($"📡 [방송 정보 변경 요청] 채널: {profile.ChzzkUid}, 제목: {newTitle}");
        return await SendCommandInternalAsync(profile.ChzzkUid, BotCommandType.UpdateTitle, newTitle, token);
    }

    public async Task<bool> UpdateCategoryAsync(StreamerProfile profile, string category, string senderUid, string? categoryId = null, string? categoryType = null, CancellationToken token = default)
    {
        _logger.LogInformation($"📡 [방송 정보 변경 요청] 채널: {profile.ChzzkUid}, 카테고리: {category}");
        return await SendCommandInternalAsync(profile.ChzzkUid, BotCommandType.UpdateCategory, category, categoryId, categoryType, token);
    }

    private async Task<bool> SendCommandInternalAsync(string chzzkUid, BotCommandType type, string? payload, string? categoryId = null, string? categoryType = null, CancellationToken token = default)
    {
        try
        {
            // [v2.6] 정밀 카테고리 업데이트를 위한 식별자(ID, Type) 포함
            var command = new ChzzkBotCommand(
                Guid.NewGuid(), 
                chzzkUid, 
                type, 
                payload, 
                categoryId,
                categoryType,
                KstClock.Now,
                "2.6"); // [v2.6] 규격 버전 업그레이드

            await _rabbitMq.PublishAsync(command, "", RabbitMqExchanges.BotCommands);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [ChzzkBotService] {chzzkUid} 명령 발행 에러 ({type}): {ex.Message}");
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
        _logger.LogInformation($"🔄 [인드라의 명령] {chzzkUid} 채널에 대한 재연결 명령을 발행합니다.");
        
        var command = new ChzzkBotCommand(
            Guid.NewGuid(), 
            chzzkUid, 
            BotCommandType.Reconnect, 
            null,
            KstClock.Now); // [v2.4.5] 누락된 타임스탬프 보강

        await _rabbitMq.PublishAsync(command, "", RabbitMqExchanges.BotCommands);
    }

    public async Task HandleAuthFailureAsync(string chzzkUid)
    {
        _logger.LogWarning($"🚨 [자가 치유 요청] {chzzkUid} 채널에 대한 토큰 새로고침 명령을 발행합니다.");
        
        var command = new ChzzkBotCommand(
            Guid.NewGuid(), 
            chzzkUid, 
            BotCommandType.RefreshSettings, 
            null,
            KstClock.Now); // [v2.4.5] 누락된 타임스탬프 보강

        await _rabbitMq.PublishAsync(command, "", RabbitMqExchanges.BotCommands);
    }

    public void CleanupRecoveryLock(string chzzkUid) { /* No-op in distributed mode */ }

}
