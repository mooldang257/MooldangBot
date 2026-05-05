using System;
using System.Collections.Generic;
using System.Linq;

public class SearchTester
{
    public static void Main()
    {
        // 사용자가 입력한 값
        string artist = "아도";
        string title = "역광";

        // AI가 변환했다고 가정하는 로직 (현재 AI 프롬프트 기반)
        // 실제로는 LLM을 거치지만, 여기서는 시뮬레이션
        string aiArtist = "Ado";
        string aiTitle = "逆光";

        Console.WriteLine($"[입력값] 가수: {artist}, 제목: {title}");
        Console.WriteLine($"[AI 변환] 가수: {aiArtist}, 제목: {aiTitle}");

        // 현재 문제가 되는 검색 조합들
        var queries = new List<string> {
            $"{artist} {title}",       // 아도 역광 -> iTunes 실패
            $"{aiArtist} {aiTitle}",   // Ado 逆光 -> iTunes 성공!
        };

        foreach (var q in queries)
        {
            Console.WriteLine($"검색 쿼리 시도: '{q}'");
        }
    }
}
