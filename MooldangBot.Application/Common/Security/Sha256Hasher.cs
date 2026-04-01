using System.Security.Cryptography;
using System.Text;

namespace MooldangBot.Application.Common.Security;

/// <summary>
/// [v4.0] 수호자의 인장: 원본 데이터를 노출하지 않고 검색 가능한 해시값을 생성합니다.
/// Application 레이어에서 공통으로 사용됩니다.
/// </summary>
public static class Sha256Hasher
{
    private const string Salt = "MooldangBot_Secure_Salt_2026";

    public static string ComputeHash(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        using var sha256 = SHA256.Create();
        var saltedInput = input + Salt;
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedInput));
        
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}
