using System;

public static class TimeUtil
{
    // 클리어 시간(ms) → "mm:ss.fff" 표기
    public static string ToClearTimeString(int millis)
    {
        TimeSpan t = TimeSpan.FromMilliseconds(millis);
        return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds:000}";
    }
}
