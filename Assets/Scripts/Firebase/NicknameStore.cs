using UnityEngine;

// 닉네임 영구 저장 (PlayerPrefs). Auth/계정과 분리된 표시용 이름.
public static class NicknameStore
{
    private const string Key = "leaderboard_nickname";
    private const string DefaultNickname = "익명";

    public const int MaxLength = 6;

    // 닉네임이 바뀔 때 발화 (로그인 버튼 라벨 등 갱신용).
    public static event System.Action OnChanged;

    public static bool HasNickname =>
        PlayerPrefs.HasKey(Key) && !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(Key));

    public static string Get()
    {
        return PlayerPrefs.GetString(Key, DefaultNickname);
    }

    public static void Set(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return;

        string trimmed = nickname.Trim();
        if (trimmed.Length > MaxLength)
            trimmed = trimmed.Substring(0, MaxLength);

        PlayerPrefs.SetString(Key, trimmed);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}
