using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MooldangBot.Infrastructure.Persistence.Converters;

/// <summary>
/// [신경망 번역기]: C#의 float[] 배열과 MariaDB 11.8의 VECTOR 타입을 연결합니다.
/// MariaDB는 "[1.0, 2.0, 3.0]" 형태의 문자열을 VECTOR 타입으로 자동 변환해 줍니다.
/// 또한 일부 드라이버에서 반환하는 바이너리(Byte[]) 형식도 지원합니다.
/// </summary>
public class MariaDbVectorConverter : ValueConverter<float[]?, byte[]?>
{
    public MariaDbVectorConverter() 
        : base(
            v => SerializeVector(v), // C# -> DB (binary)
            v => ParseVector(v)) // DB -> C# (float[])
    {
    }

    private static byte[]? SerializeVector(float[]? v)
    {
        if (v == null || v.Length == 0) return null;
        
        // [v19.0] 3072차원 데이터를 바이너리(float=4byte)로 변환하여 저장합니다.
        var bytes = new byte[v.Length * 4];
        Buffer.BlockCopy(v, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[]? ParseVector(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) return null;
        
        // [v19.0] 바이너리 데이터를 float 배열로 즉시 캐스팅합니다.
        return MemoryMarshal.Cast<byte, float>(bytes).ToArray();
    }
}
