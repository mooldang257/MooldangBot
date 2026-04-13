using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotNetEnv;

namespace MooldangBot.Simulator;

/// <summary>
/// 🚀 MooldangBot Chzzk Event Simulator v1.0
/// 이 프로그램은 치지직 공식 API의 페이로드를 모사하여 ChzzkAPI 게이트웨이에 주입합니다.
/// </summary>
class Program
{
    private static readonly HttpClient _httpClient = new();
    private static string _gatewayUrl = "http://localhost:8081";
    private static string _internalSecret = "";
    private static string _defaultChzzkUid = "";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        // 1. 환경 변수 로드
        LoadEnvironment();

        // 2. 명령줄 인자 처리 (자동화 모드)
        if (args.Length > 0)
        {
            await RunAutoMode(args);
            return;
        }

        // 3. 인터랙티브 모드 (수동 모드)
        await RunManualMode();
    }

    private static void LoadEnvironment()
    {
        try
        {
            // 상위 디렉토리의 .env 로드 시도
            Env.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".env"));
            _internalSecret = Env.GetString("INTERNAL_API_SECRET", "mooldang_osiris_secret_2026");
            _defaultChzzkUid = Env.GetString("TEST_CHZZK_UID", "");
            
            // Docker 환경 등을 고려하여 URL 조정 가능 (기본값 localhost)
            var envUrl = Env.GetString("CHZZK_GATEWAY_URL", "");
            if (!string.IsNullOrEmpty(envUrl)) _gatewayUrl = envUrl;
        }
        catch { }
    }

    private static async Task RunManualMode()
    {
        PrintHeader();
        
        Console.Write($"🔹 대상 채널 Uid (기본값: {_defaultChzzkUid}): ");
        var uid = Console.ReadLine();
        if (string.IsNullOrEmpty(uid)) uid = _defaultChzzkUid;

        if (string.IsNullOrEmpty(uid))
        {
            Console.WriteLine("❌ 채널 Uid가 필요합니다. 프로그램을 종료합니다.");
            return;
        }

        while (true)
        {
            Console.WriteLine("\n[사격 통제 장치]");
            Console.WriteLine("1. 💬 일반 채팅 발송");
            Console.WriteLine("2. 💰 채팅 후원 (1,000 치즈)");
            Console.WriteLine("3. 🎥 영상 후원 (5,000 치즈)");
            Console.WriteLine("4. 💎 티어1 신규 구독");
            Console.WriteLine("0. 🛑 시뮬레이션 종료");
            Console.Write("선택: ");

            var choice = Console.ReadLine();
            if (choice == "0") break;

            string? json = null;
            string eventName = "CHAT";

            switch (choice)
            {
                case "1":
                    Console.Write("메시지 내용: ");
                    var msg = Console.ReadLine() ?? "테스트 메시지입니다.";
                    json = PayloadTemplates.CreateChat(uid, msg);
                    eventName = "CHAT";
                    break;
                case "2":
                    json = PayloadTemplates.CreateDonation(uid, 1000, "후원 테스트입니다!", "CHAT");
                    eventName = "DONATION";
                    break;
                case "3":
                    json = PayloadTemplates.CreateDonation(uid, 5000, "영상 후원 테스트!", "VIDEO");
                    eventName = "DONATION";
                    break;
                case "4":
                    json = PayloadTemplates.CreateSubscription(uid, 1, 1);
                    eventName = "SUBSCRIPTION";
                    break;
            }

            if (json != null)
            {
                await InjectEvent(uid, eventName, json);
            }
        }
    }

    private static async Task RunAutoMode(string[] args)
    {
        // 사용법: Simulator.exe --uid {uid} --type {chat|donation|sub} --msg {content} --amount {1000}
        var uid = GetArgValue(args, "--uid") ?? _defaultChzzkUid;
        var type = GetArgValue(args, "--type")?.ToLower() ?? "chat";
        var msg = GetArgValue(args, "--msg") ?? "자동화 테스트 메시지";
        var amountStr = GetArgValue(args, "--amount") ?? "1000";
        int.TryParse(amountStr, out var amount);

        if (string.IsNullOrEmpty(uid))
        {
            Console.WriteLine("Error: --uid is required.");
            return;
        }

        string? json = type switch
        {
            "chat" => PayloadTemplates.CreateChat(uid, msg),
            "donation" => PayloadTemplates.CreateDonation(uid, amount, msg, "CHAT"),
            "video" => PayloadTemplates.CreateDonation(uid, amount, msg, "VIDEO"),
            "sub" => PayloadTemplates.CreateSubscription(uid, 1, 1),
            _ => null
        };

        string eventName = type.ToUpper() switch {
            "CHAT" => "CHAT",
            "DONATION" or "VIDEO" => "DONATION",
            "SUB" => "SUBSCRIPTION",
            _ => "CHAT"
        };

        if (json != null)
        {
            await InjectEvent(uid, eventName, json);
        }
    }

    private static async Task InjectEvent(string uid, string eventName, string json)
    {
        var request = new {
            ChzzkUid = uid,
            EventName = eventName,
            RawJson = json
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_gatewayUrl}/api/internal/test/inject")
        {
            Content = JsonContent.Create(request),
        };
        httpRequest.Headers.Add("X-Internal-Secret-Key", _internalSecret);

        try
        {
            var response = await _httpClient.SendAsync(httpRequest);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ [{eventName}] 주입 성공 (Channel: {uid})");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ 주입 실패: {response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🚨 통신 오류: {ex.Message}");
        }
    }

    private static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        }
        return null;
    }

    private static void PrintHeader()
    {
        Console.WriteLine("====================================================");
        Console.WriteLine("🌊 MooldangBot Fleet Simulator v1.0");
        Console.WriteLine("치지직 원본 파동을 재현하여 함대의 혈맥을 점검합니다.");
        Console.WriteLine("====================================================");
    }
}

public static class PayloadTemplates
{
    public static string CreateChat(string uid, string content) => $$"""
    {
        "channelId": "{{uid}}",
        "senderChannelId": "tester_uid_1234",
        "chatChannelId": "chat_channel_999",
        "profile": {
            "nickname": "시뮬레이터",
            "verifiedMark": true
        },
        "userRoleCode": "common_user",
        "content": "{{content}}",
        "messageTime": {{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}
    }
    """;

    public static string CreateDonation(string uid, int amount, string text, string type) => $$"""
    {
        "donationType": "{{type}}",
        "channelId": "{{uid}}",
        "donatorChannelId": "donator_uid_5678",
        "donatorNickname": "큰손테스터",
        "payAmount": {{amount}},
        "donationText": "{{text}}"
    }
    """;

    public static string CreateSubscription(string uid, int tier, int month) => $$"""
    {
        "channelId": "{{uid}}",
        "subscriberChannelId": "sub_uid_0000",
        "subscriberNickname": "충성구독자",
        "tierNo": {{tier}},
        "tierName": "물멍함대",
        "month": {{month}}
    }
    """;
}
