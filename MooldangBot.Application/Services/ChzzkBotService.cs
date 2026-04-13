using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Interfaces;
using MooldangBot.Contracts.Interfaces;
using MooldangBot.Application.Models;
using MooldangBot.Application.Models.Chzzk;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;

using MooldangBot.Contracts.Integrations.Chzzk.Models.Commands;

namespace MooldangBot.Application.Services;

public class ChzzkBotService : IChzzkBotService
{
    private readonly IChzzkCommandSender _commandSender;
    private readonly IDynamicQueryEngine _dynamicEngine; 
    private readonly ITokenRenewalService _renewalService;
    private readonly ILogger<ChzzkBotService> _logger;

    public ChzzkBotService(
        IChzzkCommandSender commandSender, 
        IDynamicQueryEngine dynamicEngine, 
        ITokenRenewalService renewalService,
        ILogger<ChzzkBotService> logger)
    {
        _commandSender = commandSender;
        _dynamicEngine = dynamicEngine;
        _renewalService = renewalService;
        _logger = logger;
    }


    public async Task SendReplyChatAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token)
    {
        await SendGenericChatAsync(profile, message, viewerUid, false, token);
    }

    public async Task SendReplyNoticeAsync(StreamerProfile profile, string message, string viewerUid, CancellationToken token)
    {
        await SendGenericChatAsync(profile, message, viewerUid, true, token);
    }

    public async Task<string?> GetStreamerTokenAsync(StreamerProfile profile)
    {
        // [v13.1] 통합 갱신 엔진(ITokenRenewalService)으로 일원화
        await _renewalService.RenewIfNeededAsync(profile.ChzzkUid);
        
        // 갱신 후 최신 상태의 토큰 반환 (RenewIfNeeded 내부에서 SaveChanges 수행함)
        return profile.ChzzkAccessToken;
    }

    private async Task SendGenericChatAsync(StreamerProfile profile, string message, string viewerUid, bool isNotice, CancellationToken token)
    {
        try
        {
            await Task.Delay(100, token);
            string processedMessage = await _dynamicEngine.ProcessMessageAsync(message, profile.ChzzkUid, viewerUid);

            _logger.LogInformation($"📡 [비동기 명령 발행] 대상채널: {profile.ChzzkUid}, 타입: {(isNotice ? "상단공지" : "일반")}");

            // [v4.0] 전송 시맨틱(Send) 기반 비동기 명령 발행
            ChzzkCommandBase command = isNotice 
                ? new SendChatNoticeCommand(Guid.NewGuid(), profile.ChzzkUid, DateTimeOffset.UtcNow, processedMessage)
                : new SendMessageCommand(Guid.NewGuid(), profile.ChzzkUid, DateTimeOffset.UtcNow, processedMessage);

            await _commandSender.SendAsync(command, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ [ChzzkBotService] {profile.ChzzkUid} 명령 발행 에러: {ex.Message}");
        }
    }

    public async Task UpdateTitleAsync(StreamerProfile profile, string newTitle, string senderUid, CancellationToken token)
    {
        _logger.LogInformation($"📡 [비동기 방송 정보 변경] 채널: {profile.ChzzkUid}, 제목: {newTitle}");
        var command = new UpdateTitleCommand(Guid.NewGuid(), profile.ChzzkUid, DateTimeOffset.UtcNow, newTitle);
        await _commandSender.SendAsync(command, token);
    }

    public async Task UpdateCategoryAsync(StreamerProfile profile, string category, string senderUid, string? categoryId = null, string? categoryType = null, CancellationToken token = default)
    {
        _logger.LogInformation($"📡 [비동기 방송 정보 변경] 채널: {profile.ChzzkUid}, 카테고리: {category}");
        var command = new UpdateCategoryCommand(Guid.NewGuid(), profile.ChzzkUid, DateTimeOffset.UtcNow, categoryId, categoryType, category);
        await _commandSender.SendAsync(command, token);
    }

    public async Task RefreshChannelAsync(string chzzkUid)
    {
        _logger.LogInformation($"🔄 [피닉스의 전령] {chzzkUid} 채널의 설정을 새로고침하고 연결을 점검합니다.");
        await EnsureConnectionAsync(chzzkUid);
    }

    public async Task EnsureConnectionAsync(string chzzkUid, bool forceFresh = false)
    {
        _logger.LogInformation($"🔄 [송신 엔드포인트 명령] {chzzkUid} 채널에 대한 재연결을 시도합니다.");
        
        var command = new ReconnectCommand(Guid.NewGuid(), chzzkUid, DateTimeOffset.UtcNow);
        await _commandSender.SendAsync(command);
    }

    public async Task HandleAuthFailureAsync(string chzzkUid)
    {
        _logger.LogWarning($"🚨 [송신 엔드포인트 명령] {chzzkUid} 채널에 대한 설정 새로고침 및 재연결을 시도합니다.");
        
        var command = new RefreshSettingsCommand(Guid.NewGuid(), chzzkUid, DateTimeOffset.UtcNow);
        await _commandSender.SendAsync(command);
    }

    public void CleanupRecoveryLock(string chzzkUid) { /* No-op in distributed mode */ }

}
