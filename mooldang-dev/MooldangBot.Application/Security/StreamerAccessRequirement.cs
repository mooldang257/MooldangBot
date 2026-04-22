using Microsoft.AspNetCore.Authorization;

namespace MooldangBot.Application.Security
{
    /// <summary>
    /// [오시리스의 방벽]: 특정 스트리머 채널에 대한 접근 권한 요구사항입니다.
    /// </summary>
    public class StreamerAccessRequirement : IAuthorizationRequirement { }
}
