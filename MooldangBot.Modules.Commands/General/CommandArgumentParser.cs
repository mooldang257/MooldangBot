using System.Text.RegularExpressions;
using MooldangBot.Domain.DTOs;
using MooldangBot.Domain.Entities;

namespace MooldangBot.Modules.Commands.General;

/// <summary>
/// [하모니의 해석]: 명령어 매칭 방식에 따라 메시지에서 인자(Arguments)를 체계적으로 추출합니다.
/// </summary>
public class CommandArgumentParser
{
    public string Parse(string message, CommandMetadata command)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;

        return command.MatchType switch
        {
            CommandMatchType.Prefix => ExtractPrefixArgs(message, command.Keyword),
            CommandMatchType.Contains => ExtractContainsArgs(message, command.Keyword),
            CommandMatchType.Regex => ExtractRegexArgs(message, command.Keyword),
            CommandMatchType.Exact => string.Empty,
            _ => message
        };
    }

    private string ExtractPrefixArgs(string message, string keyword)
    {
        if (message.Length <= keyword.Length) return string.Empty;
        
        // 키워드 뒤의 부분 추출 및 트리밍
        var args = message.Substring(keyword.Length).Trim();
        return args;
    }

    private string ExtractContainsArgs(string message, string keyword)
    {
        // 키워드를 제외한 메시지 전체를 반환하거나 전체를 유지
        var index = message.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return message;
        
        return (message.Remove(index, keyword.Length)).Trim();
    }

    private string ExtractRegexArgs(string message, string pattern)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(message);
            if (!match.Success) return message;

            // 캡처 그룹이 있다면 1번 그룹부터 공백으로 합쳐서 반환
            if (match.Groups.Count > 1)
            {
                var groups = new List<string>();
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    if (match.Groups[i].Success)
                        groups.Add(match.Groups[i].Value);
                }
                return string.Join(" ", groups);
            }
            
            return message;
        }
        catch
        {
            return message;
        }
    }
}
