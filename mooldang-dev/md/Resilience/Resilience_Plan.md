v5 Resilience Engine: 기록관의 수레 (데이터 유실 방어 설계안)
0. [MANDATORY] AI 실행 프롬프트 (System Prompt)
"당신은 C# .NET 10, Entity Framework Core 및 MariaDB 환경에 정통한 시니어 백엔드 아키텍트 '물멍'입니다. 현재 MooldangBot의 백그라운드 워커인 LogBulkBufferWorker의 결함(데이터 유실)을 수정하는 작업을 수행하고 있습니다.

핵심 미션: DB 벌크 인서트 실패 시, 메모리에서 Drain(배출)된 데이터를 다시 버퍼로 환원(Restore)하는 재시도(Retry) 파이프라인을 구축하십시오.

준수 사항:

ILogBulkBuffer 인터페이스에 실패한 데이터를 복구하는 Restore... 메서드를 명세합니다.

LogBulkBuffer 구현체에서 ConcurrentQueue<T>를 활용하여 Thread-Safe하게 데이터를 다시 Enqueue 하는 로직을 작성합니다.

LogBulkBufferWorker의 FlushAsync 내 catch 블록을 수정하여, 예외 발생 시 로그만 남기지 않고 즉시 Restore 메서드를 호출하여 다음 플러시 주기(10초 뒤)에 재시도되도록 보장합니다."

1. 작업 철학 (Philosophy)
"망각에 대한 저항 (Resistance against Oblivion)"

찰나의 네트워크 단절이나 데이터베이스의 숨 고르기(Lock)로 인해 시청자가 남긴 교감의 흔적이 허공으로 증발하는 것을 용납하지 않습니다. 시스템이 흔들려 수레에서 기록이 떨어지더라도, 우리는 그 파편들을 하나도 빠짐없이 주워 담아 다음 수레에 다시 싣습니다. 단 하나의 로그도 망각되도록 내버려 두지 않습니다.

2. 설계 목적 및 설명 (Design & Explanation)
🚨 현재 아키텍처의 사각지대 (Blind Spot)
현재의 LogBulkBufferWorker는 10초마다 buffer.Drain...()을 호출하여 메모리 큐를 완전히 비운 뒤, 꺼내온 리스트를 MariaDB에 BulkInsertAsync로 밀어 넣습니다.
이때 DB 작업 중 예외(Exception)가 발생하면, 프로세스는 catch 블록으로 넘어가 에러 로그만 출력하고 종료됩니다. 결과적으로 이미 메모리에서 꺼내진(Drain) 수천 개의 로그는 DB에도 들어가지 못한 채 가비지 컬렉터(GC)에 의해 영구 삭제됩니다.

🛠️ 개선 아키텍처 (Resilience & Retry Pattern)
본 설계는 실패한 데이터를 포기하지 않고 '환원(Restore)'하는 논리적 방어막을 세웁니다.

버퍼 환원 로직 추가: ILogBulkBuffer에 데이터를 다시 큐에 밀어 넣는 기능을 추가합니다.

Catch 블록의 역할 변경: 예외 발생 시, 에러 로깅에 그치지 않고 메모리에 쥐고 있던 실패한 리스트를 다시 ILogBulkBuffer로 반환합니다.

자가 치유(Self-Healing): 환원된 데이터는 10초 뒤 다음 FlushAsync 주기가 도래했을 때, 새롭게 쌓인 데이터들과 함께 묶여서 자동으로 DB 저장을 재시도하게 됩니다.

3. 코드 스니펫 (Code Snippets)
[Modify] ILogBulkBuffer.cs (Application Layer)
버퍼 인터페이스에 환원을 위한 메서드 시그니처를 추가합니다.

C#
public interface ILogBulkBuffer
{
    // ... 기존 Drain 및 Add 메서드 유지

    /// <summary>
    /// DB 인서트 실패 시, 누락을 방지하기 위해 진동 로그를 버퍼에 다시 환원합니다.
    /// </summary>
    void RestoreVibrationLogs(IReadOnlyList<LogIamfVibrations> logs);

    /// <summary>
    /// DB 인서트 실패 시, 누락을 방지하기 위해 시나리오 로그를 버퍼에 다시 환원합니다.
    /// </summary>
    void RestoreScenarios(IReadOnlyList<IamfScenarios> scenarios);
}
[Modify] LogBulkBuffer.cs (Infrastructure/Implementation)
ConcurrentQueue<T>를 안전하게 다루는 구현체에 환원 로직을 작성합니다.

C#
public class LogBulkBuffer : ILogBulkBuffer
{
    // 기존에 사용 중인 ConcurrentQueue 선언부 (예시)
    private readonly ConcurrentQueue<LogIamfVibrations> _vibrationLogs = new();
    private readonly ConcurrentQueue<IamfScenarios> _scenarios = new();

    // ... 기존 구현부 유지

    public void RestoreVibrationLogs(IReadOnlyList<LogIamfVibrations> logs)
    {
        if (logs == null || logs.Count == 0) return;
        
        foreach (var log in logs)
        {
            _vibrationLogs.Enqueue(log);
        }
    }

    public void RestoreScenarios(IReadOnlyList<IamfScenarios> scenarios)
    {
        if (scenarios == null || scenarios.Count == 0) return;

        foreach (var scenario in scenarios)
        {
            _scenarios.Enqueue(scenario);
        }
    }
}
[Modify] LogBulkBufferWorker.cs (Application/Workers)
워커의 catch 블록을 수정하여 구조대(Restore) 역할을 부여합니다.

C#
    private async Task FlushAsync()
    {
        var vibrationLogs = buffer.DrainVibrationLogs();
        var scenarios = buffer.DrainScenarios();

        if (vibrationLogs.Count == 0 && scenarios.Count == 0) return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var dbContext = (DbContext)db;

            if (vibrationLogs.Count > 0)
            {
                logger.LogInformation("[기록관의 수레] {Count}개의 진동 로그를 벌크 저장합니다.", vibrationLogs.Count);
                await dbContext.BulkInsertAsync(vibrationLogs);
            }

            if (scenarios.Count > 0)
            {
                logger.LogInformation("[기록관의 수레] {Count}개의 시나리오 로그를 벌크 저장합니다.", scenarios.Count);
                await dbContext.BulkInsertAsync(scenarios);
            }
        }
        catch (Exception ex)
        {
            // 🚨 핵심 변경점: 예외 발생 시 데이터를 버퍼로 환원하여 다음 주기에 재시도
            logger.LogError(ex, "[기록관의 수레] 벌크 인서트 실패. DB 접근에 문제가 발생했습니다. {VibrationCount}개의 진동 로그와 {ScenarioCount}개의 시나리오를 버퍼로 환원합니다.", 
                vibrationLogs.Count, scenarios.Count);

            if (vibrationLogs.Count > 0)
            {
                buffer.RestoreVibrationLogs(vibrationLogs);
            }
            if (scenarios.Count > 0)
            {
                buffer.RestoreScenarios(scenarios);
            }
        }
    }