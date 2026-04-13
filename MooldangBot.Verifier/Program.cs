using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using MooldangBot.Contracts.Events;
using MooldangBot.Contracts.Abstractions;

namespace MooldangBot.Verifier;

/// <summary>
/// [오시리스의 자가진단기]: 운영 환경에서 Contracts의 정합성을 검증하고 결과 보고서를 추출합니다.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("====================================================");
        Console.WriteLine("🔱 [MooldangBot Contract Verifier v1.0.0]");
        Console.WriteLine("함대의 새로운 혈관망 정합성을 검증합니다.");
        Console.WriteLine("====================================================");

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var reportPath = $"verification_report_{timestamp}.json";
        var results = new VerificationResults();

        try 
        {
            VerifyChatReceivedEvent(results);
            results.IsSuccess = true;
            Console.WriteLine("\n✅ 모든 계약 정합성 검토 완료!");
        }
        catch (Exception ex)
        {
            results.IsSuccess = false;
            results.ErrorMessage = ex.ToString();
            Console.WriteLine($"\n❌ 검증 중 치명적 오류 발생!");
            Console.WriteLine(ex.Message);
        }

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var jsonReport = JsonSerializer.Serialize(results, jsonOptions);
        File.WriteAllText(reportPath, jsonReport);
        
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine($"📝 검증 보고서 추출 완료: {reportPath}");
        Console.WriteLine("====================================================");
    }

    static void VerifyChatReceivedEvent(VerificationResults results)
    {
        Console.WriteLine("🔹 [ChatReceivedEvent] 검증 시작...");
        
        var @event = new ChatReceivedEvent
        {
            ChannelId = "mooldang_channel_id",
            PlatformUserId = "viewer_hash_1234",
            Nickname = "물멍_정찰기",
            Content = "!공진 지표 확인",
            PayAmount = 3000,
            UserRole = "streamer",
            IsSubscriber = true,
            SubscriptionTier = 2,
            EmojisJson = "{\"smile\": \"link_to_emoji\"}",
            CorrelationId = Guid.NewGuid().ToString()
        };

        if (@event.EventId == Guid.Empty) throw new Exception("IEvent 제약 위반: EventId가 비어있습니다.");
        if (@event.OccurredOn > DateTime.UtcNow.AddSeconds(5)) throw new Exception("IEvent 제약 위반: 발생 시간이 미래입니다.");

        var json = JsonSerializer.Serialize(@event);
        var deserialized = JsonSerializer.Deserialize<ChatReceivedEvent>(json);

        if (deserialized == null) throw new Exception("Serialization Failure: 역직렬화 결과가 null입니다.");
        if (deserialized.Nickname != @event.Nickname || deserialized.PayAmount != @event.PayAmount)
            throw new Exception("Data Loss: 직렬화 과정에서 데이터가 유실되었습니다.");

        results.PassedChecks.Add("ChatReceivedEvent: Compliance & Serialization OK");
        Console.WriteLine("  - OK: 직렬화 및 인터페이스 제약 통과");
    }
}

public class VerificationResults
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> PassedChecks { get; } = new List<string>();
    public DateTime VerifiedAt { get; } = DateTime.UtcNow;
    public string Environment { get; } = System.Environment.MachineName;
}
