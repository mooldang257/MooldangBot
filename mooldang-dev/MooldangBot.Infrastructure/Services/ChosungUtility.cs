using System.Text;

namespace MooldangBot.Infrastructure.Services;

/// <summary>
/// [v18.0] 한글 초성 추출 유틸리티 (Korean Jamo Decomposition)
/// </summary>
public static class ChosungUtility
{
    private static readonly char[] Chosungs = 
    {
        'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
    };

    /// <summary>
    /// 텍스트에서 한글 초성만 추출합니다. (공백 및 특수문자 유지)
    /// </summary>
    public static string ExtractChosung(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sb = new StringBuilder();

        foreach (char c in input)
        {
            if (c >= 0xAC00 && c <= 0xD7A3) // 한글 범위
            {
                int baseCode = c - 0xAC00;
                int chosungIndex = baseCode / (21 * 28);
                sb.Append(Chosungs[chosungIndex]);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
