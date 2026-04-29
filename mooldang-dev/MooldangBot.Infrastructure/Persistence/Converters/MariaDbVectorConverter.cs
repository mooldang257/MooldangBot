using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MooldangBot.Infrastructure.Persistence.Converters;

/// <summary>
/// [신경망 번역기]: C#의 float[] 배열과 MariaDB 11.7의 VECTOR 타입을 연결합니다.
/// MariaDB 11.7은 "[1.0, 2.0, 3.0]" 형태의 문자열을 VECTOR 타입으로 자동 변환해 줍니다.
/// 또한 일부 드라이버에서 반환하는 바이너리(Byte[]) 형식도 지원합니다.
/// </summary>
public class MariaDbVectorConverter : ValueConverter<float[], object>
{
    public MariaDbVectorConverter() 
        : base(
            v => SerializeVector(v), // C# -> DB (string)
            v => v == null ? null! : ParseVector(v)) // DB -> C# (float[])
    {
    }

    private static string SerializeVector(float[]? v)
    {
        if (v == null || v.Length == 0) return "[]";
        
        // API에서 이미 768차원으로 생성하므로 별도의 Truncate 없이 바로 직렬화합니다.
        return "[" + string.Join(",", v.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
    }

    private static float[] ParseVector(object v)
    {
        // 1. 문자열 형식 처리 (예: "[1,2,3]")
        if (v is string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<float>();
            var clean = s.Trim('[', ']', ' ');
            return clean.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(val => float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f)
                        .ToArray();
        }

        // 2. 바이너리 형식 처리 (MariaDB 11.7 VECTOR 내부 저장 형식)
        if (v is byte[] bytes)
        {
            if (bytes.Length == 0) return Array.Empty<float>();
            // float는 4바이트이므로 바이트 배열을 float 배열로 캐스팅합니다.
            return MemoryMarshal.Cast<byte, float>(bytes).ToArray();
        }

        return Array.Empty<float>();
    }
}
