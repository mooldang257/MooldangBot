using System.Text.Json;
using MooldangBot.Domain.Events;

namespace MooldangBot.Verifier.Inspectors;

/// <summary>
/// [데이터 정합 가속기]: JSON 직렬화 및 역직렬화 과정에서 데이터 유실이 없는지 검증합니다.
/// </summary>
public static class SerializationTester
{
    public static List<string> TestCoreEvents()
    {
        var logs = new List<string>();

        // 1. ChatReceivedEvent 검증
        var chatEvent = new ChatReceivedEvent
        {
            ChannelId = "test_channel",
            PlatformUserId = "user_123",
            Nickname = "Tester",
            Content = "Hello, Mooldang!",
            PayAmount = 5000,
            UserRole = "manager",
            IsSubscriber = true,
            SubscriptionTier = 1,
            EmojisJson = "{}",
            CorrelationId = Guid.NewGuid()
        };

        VerifySerialization(chatEvent);
        logs.Add("OK: ChatReceivedEvent serialization verified.");

        return logs;
    }

    private static void VerifySerialization<T>(T original) where T : class
    {
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<T>(json);

        if (deserialized == null)
            throw new Exception($"[Serialization Error] {typeof(T).Name}의 역직렬화 결과가 null입니다.");

        // 리플렉션을 이용한 동적 값 비교 (Depth 1)
        foreach (var prop in typeof(T).GetProperties())
        {
            var originalValue = prop.GetValue(original);
            var deserializedValue = prop.GetValue(deserialized);

            if (originalValue == null && deserializedValue == null) continue;
            
            if (originalValue != null && !originalValue.Equals(deserializedValue))
            {
                throw new Exception($"[Data Loss] {typeof(T).Name}.{prop.Name} 값이 일치하지 않습니다. (Original: {originalValue}, Deserialized: {deserializedValue})");
            }
        }
    }
}
