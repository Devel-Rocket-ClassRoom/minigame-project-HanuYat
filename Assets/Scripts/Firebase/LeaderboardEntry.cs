using System;

[Serializable]
public class LeaderboardEntry
{
    public string userId;
    public string nickname;
    public int timeMs; // 클리어 시간(밀리초). 낮을수록 좋음.
    public long timestamp;

    public LeaderboardEntry() { }

    public LeaderboardEntry(string userId, string nickname, int timeMs, long timestamp)
    {
        this.userId = userId;
        this.nickname = nickname;
        this.timeMs = timeMs;
        this.timestamp = timestamp;
    }

    public string ToJson()
    {
        return UnityEngine.JsonUtility.ToJson(this);
    }

    public static LeaderboardEntry FromJson(string json)
    {
        return UnityEngine.JsonUtility.FromJson<LeaderboardEntry>(json);
    }
}
