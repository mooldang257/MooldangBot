namespace MooldangAPI.Common;

public static class TimeContext
{
    private static readonly TimeZoneInfo KstZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

    public static DateTime KstNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, KstZone);

    public static DateTime ToKst(this DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, KstZone);
    }
}
