using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.DependencyInjection;
using MooldangBot.Application.Interfaces;
using MooldangBot.Application.Common.Security;
using MooldangBot.Domain.Common;
using MooldangBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 청취자]: RabbitMQ에서 채팅 이벤트를 구독하여 처리하는 POC(Proof of Concept) 서비스입니다.
/// 인프라 종속성 해결을 위해 Infrastructure 레이어에 구현되었습니다.
/// </summary>
public class RabbitMqConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConnectionFactory _factory;
    private readonly string _exchangeName = "mooldang.chat.events";

    public RabbitMqConsumerService(IConfiguration config, ILogger<RabbitMqConsumerService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        
        var host = config["RABBITMQ_HOST"] ?? "localhost";
        var port = int.TryParse(config["RABBITMQ_PORT"], out var p) ? p : 5672;
        var user = config["RABBITMQ_USER"] ?? "guest";
        var pass = config["RABBITMQ_PASS"] ?? "guest";

        _factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("[청취자의 대기] RabbitMQ 이벤트 구독을 시작합니다...");
            
            await using var connection = await _factory.CreateConnectionAsync(stoppingToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);

            // [오시리스의 익명 채널]: 임시 큐 생성
            var queueDeclareResult = await channel.QueueDeclareAsync(queue: "", exclusive: true, autoDelete: true, cancellationToken: stoppingToken);
            var queueName = queueDeclareResult.QueueName;

            await channel.QueueBindAsync(queueName, _exchangeName, string.Empty, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                try
                {
                    // 1. [Legacy/Standard] ChatEventItem 처리
                    if (message.Contains("ChzzkUid") && !message.Contains("Keyword"))
                    {
                        var eventItem = JsonSerializer.Deserialize<ChatEventItem>(message);
                        if (eventItem != null) _logger.LogDebug($"[청취자의 기록] 채팅 이벤트 수신: {eventItem.ChzzkUid}");
                    }
                    // 2. [v11.1] CommandExecutionEvent 처리 (천상의 장부 로그용)
                    else if (message.Contains("Keyword"))
                    {
                        var execEvent = JsonSerializer.Deserialize<CommandExecutionEvent>(message);
                        if (execEvent != null)
                        {
                            await SaveCommandLogAsync(execEvent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[청취자의 혼란] 메시지 역직렬화 또는 인스턴트 저장 중 오류 발생");
                }

                await Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation($"[청취자의 안착] 전용 큐({queueName})를 통해 익스체인지({_exchangeName})를 구독 중입니다.");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[청취자의 침묵] RabbitMQ 소비자 기동 중 오류가 발생했습니다. (서버 부재 가능성)");
        }
    }

    /// <summary>
    /// [천상의 장부]: 명령어 실행 이력을 DB에 영속화합니다.
    /// </summary>
    private async Task SaveCommandLogAsync(CommandExecutionEvent e)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // [v11.1] 비동기 데이터 정규화: ChzzkUid -> StreamerProfileId 매핑
        var streamerId = await db.StreamerProfiles
            .Where(s => s.ChzzkUid == e.ChzzkUid)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (streamerId == 0) return;

        // GlobalViewerId 조회 (해시 기반)
        var viewerHash = MooldangBot.Application.Common.Security.Sha256Hasher.ComputeHash(e.SenderId ?? "");
        var globalViewerId = await db.GlobalViewers
            .Where(g => g.ViewerUidHash == viewerHash)
            .Select(g => g.Id)
            .FirstOrDefaultAsync();

        var log = new CommandExecutionLog
        {
            StreamerProfileId = streamerId,
            GlobalViewerId = globalViewerId,
            Keyword = e.Keyword,
            IsSuccess = e.IsSuccess,
            ErrorMessage = e.ErrorMessage,
            DonationAmount = e.DonationAmount ?? 0,
            CreatedAt = KstClock.Now
        };

        db.CommandExecutionLogs.Add(log);
        await db.SaveChangesAsync();
        
        _logger.LogDebug($"📉 [천상의 장부 기록] 명령어 '{e.Keyword}' 실행 이력 저장 완료 (Streamer: {e.ChzzkUid})");
    }
}
