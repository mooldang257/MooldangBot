using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using MooldangBot.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MooldangBot.Application.Common.Security;

/// <summary>
/// [오시리스의 무지개]: 16자리 짧은 해시 토큰을 기반으로 오버레이 접속을 인증합니다.
/// (Legacy JWT와 공존하기 위한 커스텀 인증 핸들러)
/// </summary>
public class OverlayShortTokenHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceScopeFactory _scopeFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. 쿼리 스트링에서 access_token 추출
        string? token = Request.Query["access_token"];
        
        // [시니어 팁]: 토큰이 없거나 16자리가 아니면 이 핸들러는 처리하지 않고 넘깁니다. (JWT 핸들러가 처리하도록)
        if (string.IsNullOrEmpty(token) || token.Length != 16)
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            // 2. DB에서 해당 토큰을 가진 스트리머 조회
            var streamer = await db.StreamerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OverlayToken == token);

            if (streamer == null)
            {
                return AuthenticateResult.Fail("[오시리스의 거부] 유효하지 않은 오버레이 토큰입니다.");
            }

            // 3. 인증 성공: 클레임 생성 (JWT와 동일한 규격 유지)
            var claims = new List<Claim>
            {
                new Claim("StreamerId", streamer.ChzzkUid.ToLower()),
                new Claim(ClaimTypes.Role, "Streamer"),
                new Claim("TokenMode", "ShortHash")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(ex.Message);
        }
    }
}
