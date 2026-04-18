using System.Text;
using System.Text.RegularExpressions;

namespace MooldangBot.Application.Common.Utils;

/// <summary>
/// [v13.0] 함교 지능형 언어 처리 및 별칭 추출 유틸리티 (Linguistic Resonance)
/// </summary>
public static class KoreanUtils
{
    private static readonly char[] ChosungList = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ".ToCharArray();

    // [v13.0] 별칭에서 제외할 쓰레기 단어 블랙리스트 (HashSet으로 O(1) 검색 최적화)
    private static readonly HashSet<string> TrashBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "official", "mv", "music video", "lyric video", "audio", "live", "1시간", "1 hour", 
        "가사", "lyrics", "교차편집", "stage mix", "세로직캠", "fancam", "official video",
        "mr", "inst", "instrumental", "tv", "vocal", "dance", "cover"
    };

    // [v13.0] 1차 노이즈 제거용 정규식
    private static readonly Regex NoiseRegex = new(
        @"(?i)\[?(official mv|music video|lyric video|audio|live|1시간|1 hour|lyrics|가사|교차편집|stage mix|fancam)\]?",
        RegexOptions.Compiled);

    // [v13.0] 괄호 및 홀따옴표 내 텍스트 추출용 정규식
    private static readonly Regex AliasExtractor = new(
        @"[\(\[\'\‘\“](.*?)[\)\]\'\’\”]",
        RegexOptions.Compiled);

    /// <summary>
    /// 검색을 위한 텍스트 정규화 (영문 소문자화 + 한글 초성 추출)
    /// </summary>
    public static string NormalizeForSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sb = new StringBuilder();
        var normalized = input.ToLowerInvariant().Trim();

        foreach (char c in normalized)
        {
            // 한글 유니코드 범위: 0xAC00 ~ 0xD7A3
            if (c >= 0xAC00 && c <= 0xD7A3)
            {
                int baseCode = c - 0xAC00;
                int chosungIndex = baseCode / (21 * 28);
                sb.Append(ChosungList[chosungIndex]);
            }
            else
            {
                // 한글이 아닌 경우(영문, 숫자, 공백 등)는 그대로 소문자 유지
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// [v13.0] 메타데이터(유튜브 등)에서 잠재적 별칭 리스트 추출
    /// </summary>
    public static List<string> ExtractSmartAliases(string? youtubeTitle, string originalTitle, string artist)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(youtubeTitle)) return result.ToList();

        // 1. 선(先) 정제: 노이즈 제거
        var cleaned = NoiseRegex.Replace(youtubeTitle, " ").Trim();

        // 2. 괄호/홀따옴표 내부 추출
        var matches = AliasExtractor.Matches(cleaned);
        foreach (Match match in matches)
        {
            var candidate = match.Groups[1].Value.Trim();

            // 3. 후(後) 필터링 (블랙리스트 & 길이 & 중복 검증)
            if (candidate.Length >= 2 && 
                !TrashBlacklist.Contains(candidate) &&
                !candidate.Equals(originalTitle, StringComparison.OrdinalIgnoreCase) &&
                !candidate.Equals(artist, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(candidate);
            }
        }

        return result.ToList();
    }
}
