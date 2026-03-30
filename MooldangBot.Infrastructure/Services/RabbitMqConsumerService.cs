using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooldangBot.Application.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [오시리스의 청취자]: RabbitMQ에서 채팅 이벤트를 구독하여 처리하는 POC(Proof of Concept) 서비스입니다.
/// 인프라 종속성 해결을 위해 Infrastructure 레이어에 구현되었습니다.
/// </summary>
public class RabbitMqConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly ConnectionFactory _factory;
    private readonly string _exchangeName = "mooldang.chat.events";

    public RabbitMqConsumerService(IConfiguration config, ILogger<RabbitMqConsumerService> logger)
    {
        _logger = logger;
        
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
                    var eventItem = JsonSerializer.Deserialize<ChatEventItem>(message);
                    if (eventItem != null)
                    {
                        _logger.LogDebug($"[청취자의 기록] RabbitMQ로부터 이벤트 수신: {eventItem.ChzzkUid}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[청취자의 혼란] 메시지 역직렬화 중 오류 발생");
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
}
