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
        Console.WriteLine("🔱 [MooldangBot Contract Verifier v1.1.0]");
        Console.WriteLine("함대의 새로운 혈관망 정합성을 정밀 검증합니다.");
        Console.WriteLine("[Mode: Standalone / EDMH Phase 0]");
        Console.WriteLine("====================================================");

        var reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");
        if (!Directory.Exists(reportDir)) Directory.CreateDirectory(reportDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var reportPath = Path.Combine(reportDir, $"verification_report_{timestamp}.json");
        var results = new VerificationResults();

        try 
        {
            // 1. 계약 준수 자가 진단 (Reflection Audit)
            Console.WriteLine("\n[1/2] 계약 준수 사항 전수 조사 중...");
            var auditLogs = Inspectors.ContractInspector.Audit(typeof(ChatReceivedEvent).Assembly);
            results.PassedChecks.AddRange(auditLogs);
            Console.WriteLine("  - 완료: 모든 계약이 IEvent 인터페이스를 준수함.");

            // 2. 직렬화 정합성 테스트 (Serialization Test)
            Console.WriteLine("\n[2/2] 데이터 직렬화 정합성 테스트 중...");
            var serializationLogs = Inspectors.SerializationTester.TestCoreEvents();
            results.PassedChecks.AddRange(serializationLogs);
            Console.WriteLine("  - 완료: 핵심 이벤트의 데이터 유실 없음.");

            results.IsSuccess = true;
            Console.WriteLine("\n✅ 모든 계약 및 데이터 정합성 검증 성공!");
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
}

public class VerificationResults
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> PassedChecks { get; } = new List<string>();
    public DateTime VerifiedAt { get; } = DateTime.UtcNow;
    public string Environment { get; } = System.Environment.MachineName;
}
