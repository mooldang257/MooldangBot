using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MooldangBot.Domain.Common;

/// <summary>
/// [시간의 파울]: 도메인 전역에서 KST(UTC+9) 시간을 보장하는 독립 자료형입니다. (v2.0)
/// DateTime과의 혼선을 방지하고 크로스 플랫폼(Windows/Linux) 호환성을 지원합니다.
/// </summary>
[JsonConverter(typeof(KstClockJsonConverter))]
public readonly record struct KstClock : IFormattable, IComparable<KstClock>
{
    public int CompareTo(KstClock other) => Value.CompareTo(other.Value);

    public DateTime Value { get; }
    public long Ticks => Value.Ticks;
    public DateTime Date => Value.Date;

    public static KstClock FromTicks(long ticks) => new(new DateTime(ticks, DateTimeKind.Unspecified));
    
    public static KstClock MinValue => FromDateTime(DateTime.MinValue);
    public static KstClock MaxValue => FromDateTime(DateTime.MaxValue);
    
    private static readonly TimeZoneInfo KstTimeZone = GetKstTimeZone();

    private KstClock(DateTime value) => Value = value;

    private static TimeZoneInfo GetKstTimeZone()
    {
        try 
        { 
            // 1. IANA 표준 (Linux/Mac/Docker)
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul"); 
        }
        catch 
        { 
            try 
            {
                // 2. Windows Fallback
                return TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"); 
            }
            catch
            {
                // 3. [v19.5] 최종 수단: 시스템에 타임존 데이터가 없을 경우 강제 생성 (UTC+9)
                return TimeZoneInfo.CreateCustomTimeZone("KST", TimeSpan.FromHours(9), "KST", "KST");
            }
        }
    }

    /// <summary>KstClock에서 DateTime으로의 암시적 형변환을 지원합니다.</summary>
    public static implicit operator DateTime(KstClock kst) => kst.Value;

    /// <summary>DateTime에서 KstClock으로의 명시적 형변환을 지원합니다. (권장: FromDateTime)</summary>
    public static explicit operator KstClock(DateTime dt) => FromDateTime(dt);

    /// <summary>
    /// 지정된 DateTime을 KST 기준으로 안전하게 변환하여 KstClock 인스턴스를 생성합니다.
    /// Utc인 경우 KST로 변환하며, 그 외의 경우 KST 값으로 간주합니다.
    /// </summary>
    public static KstClock FromDateTime(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
        {
            return new KstClock(TimeZoneInfo.ConvertTimeFromUtc(dt, KstTimeZone));
        }
        // 내부 Value는 항상 Unspecified로 유지하여 동등성 비교 최적화
        return new KstClock(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified));
    }

    /// <summary>현재 시각(KST)을 반환합니다.</summary>
    public static KstClock Now => FromDateTime(DateTime.UtcNow);

    /// <summary>오늘 날짜(KST)를 반환합니다.</summary>
    public static KstClock Today => new(Now.Value.Date);

    // 비교 연산자 오버로딩 (Type-safe 비교 지원)
    public static bool operator >(KstClock left, KstClock right) => left.Value > right.Value;
    public static bool operator <(KstClock left, KstClock right) => left.Value < right.Value;
    public static bool operator >=(KstClock left, KstClock right) => left.Value >= right.Value;
    public static bool operator <=(KstClock left, KstClock right) => left.Value <= right.Value;

    // 시간 연산 지원
    public KstClock AddSeconds(double value) => new(Value.AddSeconds(value));
    public KstClock AddMinutes(double value) => new(Value.AddMinutes(value));
    public KstClock AddHours(double value) => new(Value.AddHours(value));
    public KstClock AddDays(double value) => new(Value.AddDays(value));
    
    // [v2.0] 토큰 만료 임계치 체크용 헬퍼
    public bool IsExpiringSoon(int minutes = 5) => Value < Now.Value.AddMinutes(minutes);

    public static bool IsExpiringSoon(KstClock? expiresAt, TimeSpan threshold) 
        => expiresAt.HasValue && expiresAt.Value.Value < Now.Value.Add(threshold);

    // KstClock - KstClock = TimeSpan 지원
    public static TimeSpan operator -(KstClock left, KstClock right) => left.Value - right.Value;
    public static TimeSpan operator -(KstClock left, DateTime right) => left.Value - right;

    public override string ToString() => Value.ToString("yyyy-MM-dd HH:mm:ss");
    public string ToString(string? format, IFormatProvider? formatProvider = null) => Value.ToString(format, formatProvider);
}

/// <summary>
/// KstClock 전용 JSON 컨버터입니다. (ISO-8601 +09:00 오프셋 명시)
/// </summary>
public class KstClockJsonConverter : JsonConverter<KstClock>
{
    public override KstClock Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => KstClock.FromDateTime(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, KstClock value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss+09:00"));
}
