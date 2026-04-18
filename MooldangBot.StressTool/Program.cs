using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using DotNetEnv;

namespace MooldangBot.StressTool;

// [v2.4.3] 함대 최종 병기: 실시간 부하 및 멱등성 검증 툴
class Program
{
    private static string _chzzkUid = "";
    private static string _rabbitHost = "192.168.219.103";
    private static string _rabbitUser = "mooldang_guest";
    private static string _rabbitPass = "mooldang1234!";
    private static string _exchange = "MooldangBot.ChatEvents";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("====================================================");
        Console.WriteLine("🚀 [MooldangBot Fleet StressTool v2.4.6]");
        Console.WriteLine("함대의 무결성과 맷집을 한계까지 몰아붙입니다.");
        Console.WriteLine("====================================================");

        // 1. 환경 설정 로드 (.env)
        var envPaths = new[] 
        { 
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".env") // IDE 실행 대비
        };

        bool envLoaded = false;
        foreach (var path in envPaths)
        {
            if (File.Exists(path))
            {
                Env.Load(path);
                Console.WriteLine($"✅ Environment loaded from: {path}");
                envLoaded = true;
                break;
            }
        }

        if (!envLoaded) Console.WriteLine("⚠️ Warning: .env file not found. Falling back to defaults.");

        try
        {
            _rabbitHost = Env.GetString("RABBITMQ_HOST", "localhost");
            if (_rabbitHost == "rabbitmq") _rabbitHost = "localhost";
            
            _rabbitUser = Env.GetString("RABBITMQ_USER", "guest");
            _rabbitPass = Env.GetString("RABBITMQ_PASS", "guest");
            _chzzkUid = Env.GetString("TEST_CHZZK_UID", "");

            Console.WriteLine($"🔹 Config: Host={_rabbitHost}, User={_rabbitUser}, TargetUID={_chzzkUid}");
        }
        catch { /* Fallback to defaults */ }

        // 2. 대상 채널 입력
        Console.Write($"🔹 대상 Chzzk UID (기본값: {_chzzkUid}): ");
        var inputUid = Console.ReadLine();
        if (!string.IsNullOrEmpty(inputUid)) _chzzkUid = inputUid;

        if (string.IsNullOrEmpty(_chzzkUid))
        {
            Console.WriteLine("❌ 대상 UID가 지정되지 않았습니다. 종료합니다.");
            return;
        }

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
        var json = JsonConvert.SerializeObject(eventItem);
        var body = Encoding.UTF8.GetBytes(json);
        
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        // v2.0 규격 라우팅 키: streamer.{uid}.chat
        var routingKey = $"streamer.{_chzzkUid}.chat";
        
        // [v2.4.6] 봇 엔진 기동 전에도 테스트 가능하도록 교환기 자동 선언
        await channel.ExchangeDeclareAsync(exchange: _exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);

        await channel.BasicPublishAsync(
            exchange: _exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    private static string CreateChatPayload(string chzzkUid, string content, string senderId = "stress_test_bot", string nickname = "스트레스정찰기")
    {
        // [v2.4.6] 최신 치지직 원본 JSON 규격 시뮬레이션
        // 핸들러가 기대하는 ChzzkChatEventPayload 필드: content, senderChannelId, profile(JSON)
        var profileJson = JsonConvert.SerializeObject(new
        {
            nickname = nickname,
            userRoleCode = "common_user"
        });

        var payload = new object[]
        {
            "CHAT",
            JsonConvert.SerializeObject(new
            {
                content = content,
                senderChannelId = senderId,
                channelId = chzzkUid,
                profile = profileJson
            })
        };
        return JsonConvert.SerializeObject(payload);
    }
}
