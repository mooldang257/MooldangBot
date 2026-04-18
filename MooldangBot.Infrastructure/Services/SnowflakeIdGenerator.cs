using IdGen;
using MooldangBot.Contracts.Common.Interfaces;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [v13.1] IdGen 라이브러리를 활용한 Snowflake ID 생성기 구현체입니다.
/// </summary>
public class SnowflakeIdGenerator : ISongLibraryIdGenerator
{
    private readonly IdGenerator _generator;

    public SnowflakeIdGenerator()
    {
        // 머신 ID(0) 설정. 향후 환경변수로 분리 가능.
        _generator = new IdGenerator(0); 
    }

    /// <inheritdoc />
    public long GenerateNewId()
    {
        return _generator.CreateId();
    }
}
