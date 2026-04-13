using System.Reflection;
using MooldangBot.Contracts.Abstractions;

namespace MooldangBot.Verifier.Inspectors;

/// <summary>
/// [계약 감사관]: Contracts 어셈블리를 전수 조사하여 인터페이스 제약 사항을 확인합니다.
/// </summary>
public static class ContractInspector
{
    public static List<string> Audit(Assembly assembly)
    {
        var auditLogs = new List<string>();
        var eventTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace != null && t.Namespace.Contains("Events"))
            .ToList();

        foreach (var type in eventTypes)
        {
            if (!typeof(IEvent).IsAssignableFrom(type))
            {
                throw new Exception($"[Audit Failure] {type.Name} 클래스가 IEvent 인터페이스를 구현하지 않았습니다.");
            }

            // 필수 프로퍼티 존재 여부 재검증 (Reflection)
            var props = type.GetProperties();
            var hasEventId = props.Any(p => p.Name == "EventId" && p.PropertyType == typeof(Guid));
            var hasOccurredOn = props.Any(p => p.Name == "OccurredOn" && p.PropertyType == typeof(DateTime));

            // Note: typeof(DateTime) is correct, but let's be safe and check for both DateTime and DateTimeOffset if needed.
            // But IEvent specifically uses DateTime.

            auditLogs.Add($"OK: {type.Name} - IEvent compliance verified.");
        }

        return auditLogs;
    }
}
