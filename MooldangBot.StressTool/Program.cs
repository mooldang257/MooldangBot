using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Newtonsoft.Json;
using DotNetEnv;

namespace MooldangBot.StressTool;

class Program
{
    private static string _chzzkUid = "";
    private static string _rabbitHost = "localhost";
    private static string _rabbitUser = "guest";
    private static string _rabbitPass = "guest";
    private static string _exchange = "MooldangBot.ChatEvents";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("====================================================");
        Console.WriteLine("🚀 [MooldangBot Fleet StressTool v2.4.6]");
        Console.WriteLine("함대의 무결성과 맷집을 한계까지 몰아붙입니다.");
        Console.WriteLine("====================================================");

        try
        {
            // 1. 환경 설정 로드 (.env)
            try
            {
                var localEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
                var parentEnv = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
                
                if (File.Exists(localEnv)) Env.Load(localEnv);
                else if (File.Exists(parentEnv)) Env.Load(parentEnv);

                _rabbitHost = Env.GetString("RABBITMQ_HOST", "localhost");
                _rabbitUser = Env.GetString("RABBITMQ_USER", "guest");
                _rabbitPass = Env.GetString("RABBITMQ_PASS", "guest");
                _chzzkUid = Env.GetString("TEST_CHZZK_UID", "");

                Console.WriteLine($"🔍 설정 로드됨: RabbitMQ({_rabbitHost}), User({_rabbitUser})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ .env 로드 중 오류 발생 (기본값 사용): {ex.Message}");
            }

            // 2. 대상 채널 입력
            if (string.IsNullOrEmpty(_chzzkUid))
            {
                Console.Write($"🔹 대상 Chzzk UID를 입력하세요: ");
                var inputUid = Console.ReadLine();
                if (!string.IsNullOrEmpty(inputUid)) _chzzkUid = inputUid;
            }
            else
            {
                Console.WriteLine($"🔹 대상 Chzzk UID: {_chzzkUid}");
            }

            if (string.IsNullOrEmpty(_chzzkUid))
            {
                Console.WriteLine("❌ 대상 UID가 지정되지 않았습니다. 종료합니다.");
            }
            else
            {
                // 3. 메인 전술 루프
                while (true)
                {
                    Console.WriteLine("\n[전술 선택]");
                    Console.WriteLine("1. 🧨 Idempotency Bomb (멱등성 가중 테스트: 동일 ID 10회 난사)");
                    Console.WriteLine("2. 🌊 Fleet Flood (대규모 채팅 시뮬레이션: TPS 조절 가능)");
                    Console.WriteLine("3. 🅿️ Point Stress (포인트 명령어 난사: DB 원자성 검증)");
                    Console.WriteLine("0. 🛑 작전 종료");
                    Console.Write("선택: ");

                    var choice = Console.ReadLine();
                    if (choice == "0") break;

                    switch (choice)
                    {
                        case "1":
                            await ExecuteIdempotencyBomb();
                            break;
                        case "2":
                            await ExecuteFleetFlood();
                            break;
                        case "3":
                            await ExecutePointStress();
                            break;
                        default:
                            Console.WriteLine("⚠️ 잘못된 선택입니다.");
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("\n❌ [시스템 치명적 오류 발생]");
            Console.WriteLine($"메시지: {ex.Message}");
            Console.WriteLine("--- 상세 로그 ---");
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            Console.WriteLine("\n====================================================");
            Console.WriteLine("종료하려면 아무 키나 누르세요...");
            Console.ReadKey();
        }
    }

    private static async Task ExecuteIdempotencyBomb()
    {
        Console.WriteLine("\n[🧨 Idempotency Bomb 작전 개시]");
        var correlationId = Guid.NewGuid();
        Console.WriteLine($"Generated CorrelationId: {correlationId}");

        using var connection = await CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // 동일한 ID로 10번 전송
        for (int i = 1; i <= 10; i++)
        {
            var payload = CreateChatPayload(_chzzkUid, $"[StressTest] 멱등성 폭격 {i}/10");
            var eventItem = new
            {
                MessageId = correlationId,
                ChzzkUid = _chzzkUid,
                JsonPayload = payload,
                ReceivedAt = DateTime.Now,
                Version = "2.2"
            };

            await PublishMessageAsync(channel, eventItem);
            Console.WriteLine($"  - [{i}/10] 발송 완료");
        }

        Console.WriteLine("✅ 폭격 완료. Grafana 대시보드에서 'Blocked' 수치가 9 상승했는지 확인하세요.");
    }

    private static async Task ExecuteFleetFlood()
    {
        Console.Write("\n🔹 초당 메시지 발송 수 (TPS, 기본 10): ");
        if (!int.TryParse(Console.ReadLine(), out int tps)) tps = 10;

        Console.Write("🔹 총 지속 시간 (초, 기본 5): ");
        if (!int.TryParse(Console.ReadLine(), out int duration)) duration = 5;

        Console.WriteLine($"\n[🌊 Fleet Flood 작전 개시] {tps} TPS / {duration}s");

        using var connection = await CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        var totalCount = tps * duration;
        var interval = 1000 / tps;

        for (int i = 1; i <= totalCount; i++)
        {
            var eventItem = new
            {
                MessageId = Guid.NewGuid(),
                ChzzkUid = _chzzkUid,
                JsonPayload = CreateChatPayload(_chzzkUid, $"[Flood] 함대 부하 테스트 메시지 {i}"),
                ReceivedAt = DateTime.Now,
                Version = "2.2"
            };

            await PublishMessageAsync(channel, eventItem);
            if (i % 10 == 0) Console.Write(".");
            await Task.Delay(interval);
        }

        Console.WriteLine($"\n✅ 홍수 작전 종료. 총 {totalCount}개 메시지 발송됨.");
    }

    private static async Task ExecutePointStress()
    {
        Console.WriteLine("\n[🅿️ Point Stress 작전 개시]");
        Console.Write("🔹 발송할 명령어 (기본 !룰렛): ");
        var cmd = Console.ReadLine();
        if (string.IsNullOrEmpty(cmd)) cmd = "!룰렛";

        using var connection = await CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // 20개의 서로 다른 사용자가 동시에 명령어를 사용하는 상황 시뮬레이션
        for (int i = 1; i <= 20; i++)
        {
            var senderId = $"test_user_{i}";
            var eventItem = new
            {
                MessageId = Guid.NewGuid(),
                ChzzkUid = _chzzkUid,
                JsonPayload = CreateChatPayload(_chzzkUid, cmd, senderId: senderId, nickname: $"테스터_{i}"),
                ReceivedAt = DateTime.Now,
                Version = "2.2"
            };

            await PublishMessageAsync(channel, eventItem);
            Console.WriteLine($"  - [User_{i}] '{cmd}' 명령 발송 완료");
        }

        Console.WriteLine("✅ 포인트 스트레스 작전 완료. DB 잔액의 정밀 정합성을 확인하세요.");
    }

    private static async Task<IConnection> CreateConnectionAsync()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _rabbitHost,
            UserName = _rabbitUser,
            Password = _rabbitPass
        };
        return await factory.CreateConnectionAsync();
    }

    private static async Task PublishMessageAsync(IChannel channel, object eventItem)
    {
        // [v2.4.6] 교환기가 없을 경우를 대비해 직접 선언 (Topic 타입)
        await channel.ExchangeDeclareAsync(exchange: _exchange, type: ExchangeType.Topic, durable: true);

        var json = JsonConvert.SerializeObject(eventItem);
        var body = Encoding.UTF8.GetBytes(json);
        
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: _exchange,
            routingKey: "chat.event", 
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    private static string CreateChatPayload(string channelId, string content, string senderId = "test_user_777", string nickname = "무력한시청자")
    {
        var payload = new 
        {
            channelId = channelId,
            senderId = senderId,
            nickname = nickname,
            content = content,
            receivedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        return JsonConvert.SerializeObject(payload);
    }
}
