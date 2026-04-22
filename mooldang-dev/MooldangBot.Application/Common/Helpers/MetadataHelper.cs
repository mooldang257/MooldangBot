using System;
using System.Security.Cryptography;
using System.Text;

namespace MooldangBot.Application.Common.Helpers;

/// <summary>
/// [v12.5] 곡 정보(제목, 가수)를 바탕으로 데이터베이스 전체에서 통용되는 고유 메타데이터 키를 생성합니다.
/// </summary>
public static class MetadataHelper
{
    /// <summary>
    /// 제목과 가수를 무자비하게 정규화(공백/탭/특수문자 제거, 소문자화)한 후 SHA256 해시 키를 생성합니다.
    /// </summary>
    public static string GenerateMetadataKey(string title, string? artist)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        // 1. 소문자화 및 기본 결합
        var combined = $"{title}|{artist ?? ""}".ToLowerInvariant();

        // 2. 무자비한 정제 (Ruthless Normalization): 알파벳, 숫자, 한글, '|' 만 남기고 모두 제거 (공백/탭/특수문자 박멸)
        var sb = new StringBuilder();
        foreach (var c in combined)
        {
            if (char.IsLetterOrDigit(c) || c == '|') 
            {
                sb.Append(c);
            }
        }

        var pureText = sb.ToString();

        // 3. 퓨어 텍스트 기반 SHA256 해시 생성
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(pureText);
        var hashBytes = sha256.ComputeHash(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
