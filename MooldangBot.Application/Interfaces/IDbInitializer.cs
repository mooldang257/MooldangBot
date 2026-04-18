using System.Threading.Tasks;

namespace MooldangBot.Contracts.Common.Interfaces;

/// <summary>
/// [오시리스의 시동]: 애플리케이션 시작 시 데이터베이스 초기화 및 초기 서비스 기동을 담당합니다.
/// </summary>
public interface IDbInitializer
{
    Task InitializeAsync();
}
