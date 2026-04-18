using System;

namespace MooldangBot.Domain.Common;

/// <summary>
/// [오시리스의 거절]: 리프레시 토큰이 만료되거나 권한이 박탈되어 
/// 자동화된 자가 치유(Retry)가 불가능한 치명적 상태를 나타냅니다.
/// </summary>
public class FatalTokenException : Exception
{
    public FatalTokenException(string message) : base(message) { }
    public FatalTokenException(string message, Exception innerException) : base(message, innerException) { }
}
