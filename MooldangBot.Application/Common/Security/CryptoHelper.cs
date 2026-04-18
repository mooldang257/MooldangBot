using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace MooldangBot.Application.Common.Security;

/// <summary>
/// [오시리스의 인장]: OAuth 2.0 PKCE 및 보안을 위한 암호화 유틸리티입니다.
/// </summary>
public static class CryptoHelper
{
    /// <summary>
    /// PKCE Code Verifier를 생성합니다. (43~128자 사이의 랜덤 문자열)
    /// </summary>
    public static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Code Verifier로부터 Code Challenge (S256)를 생성합니다.
    /// </summary>
    public static string GenerateCodeChallenge(string verifier)
    {
        var verifierBytes = Encoding.UTF8.GetBytes(verifier);
        var hash = SHA256.HashData(verifierBytes);
        
        // [물멍의 제언]: Microsoft.AspNetCore.WebUtilities.WebEncoders를 사용하여 패딩(=) 없이 인코딩
        return WebEncoders.Base64UrlEncode(hash);
    }
}
