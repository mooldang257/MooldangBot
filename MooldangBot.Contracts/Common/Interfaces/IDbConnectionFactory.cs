using System.Data;

namespace MooldangBot.Contracts.Common.Interfaces;

/// <summary>
/// [v7.0] Dapper 등의 라이브러리에서 사용할 DB 커넥션을 생성하는 팩토리 인터페이스입니다.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// 새로운 DB 커넥션을 생성하여 반환합니다. 
    /// 반환된 커넥션은 호출자가 using 블록 등으로 관리해야 합니다.
    /// </summary>
    IDbConnection CreateConnection();
}
