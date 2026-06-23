using System;

public static class TimeUtil
{
    public static long NowUnixMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public static DateTime FromUnixMillis(long millis)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(millis).LocalDateTime;
    }

    public static string ToDateString(long millis)
    {
        return FromUnixMillis(millis).ToString("yyyy-MM-dd HH:mm:ss");
    }

    // 클리어 시간(ms) → "mm:ss.fff" 표기
    public static string ToClearTimeString(int millis)
    {
        TimeSpan t = TimeSpan.FromMilliseconds(millis);
        return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds:000}";
    }
}
