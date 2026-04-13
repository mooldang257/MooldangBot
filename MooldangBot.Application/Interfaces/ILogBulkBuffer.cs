using MooldangBot.Domain.Entities.Philosophy;
using System.Collections.Concurrent;

namespace MooldangBot.Application.Interfaces;

/// <summary>
/// [오시리스의 항아리]: 대량으로 발생하는 로그를 메모리에 임시 보관했다가 벌크로 쏟아내기 위한 버퍼 인터페이스입니다.
/// </summary>
public interface ILogBulkBuffer
{
    void AddVibrationLog(IamfVibrationLog log);
    void AddScenario(IamfScenario scenario);
    
    List<IamfVibrationLog> DrainVibrationLogs();
    List<IamfScenario> DrainScenarios();
}
