using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

// 최단 클리어 시간 리더보드. timeMs 낮을수록 상위.
public class LeaderboardManager : MonoBehaviour
{
    private static LeaderboardManager instance;
    public static LeaderboardManager Instance => instance;

    private DatabaseReference leaderboardRef;
    private Query listenerQuery;

    private bool isListenerActive;
    public event Action<List<LeaderboardEntry>> OnLeaderboardUpdated;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private async UniTaskVoid Start()
    {
        if (!await FirebaseInitializer.Instance.WaitForInitializationAsync())
        {
            Debug.LogError($"[Leaderboard] 파이어베이스 초기화 실패!");
            return;
        }

        leaderboardRef = FirebaseInitializer.Instance.Database.RootReference.Child("leaderboard");
    }

    // 기존 기록보다 빠를 때만 저장. saved=false 면 더 느려서 갱신 안 함.
    public async UniTask<(bool success, bool saved, string error)> SubmitTimeAsync(int timeMs)
    {
        if (!AuthManager.Instance.IsLogedIn)
        {
            return (false, false, "로그인이 필요합니다.");
        }

        if (leaderboardRef == null)
        {
            return (false, false, "leaderboardRef == null");
        }

        if (timeMs <= 0)
        {
            return (false, false, "잘못된 클리어 시간입니다.");
        }

        string userId = AuthManager.Instance.UserId;
        string nickname = NicknameStore.Get();

        try
        {
            DatabaseReference entryRef = leaderboardRef.Child(userId);

            // 기존 기록 조회 → 더 느리면 스킵
            DataSnapshot existing = await entryRef.Child("timeMs").GetValueAsync();
            if (existing.Exists && int.TryParse(existing.Value.ToString(), out int prevMs))
            {
                if (timeMs >= prevMs)
                {
                    Debug.Log($"[Leaderboard] 기존 기록({prevMs}ms)이 더 빠름 → 저장 스킵");
                    return (true, false, null);
                }
            }

            Debug.Log($"[Leaderboard] 신기록 저장 시도... ({timeMs}ms)");

            Dictionary<string, object> entryData = new Dictionary<string, object>
            {
                { "userId", userId },
                { "nickname", nickname },
                { "timeMs", timeMs },
                { "timestamp", ServerValue.Timestamp },
            };
            await entryRef.UpdateChildrenAsync(entryData);

            Debug.Log($"[Leaderboard] 신기록 저장 성공");
            return (true, true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Leaderboard] 리더보드 저장 실패! {ex.Message}");
            return (false, false, ex.Message);
        }
    }

    public async UniTask<List<LeaderboardEntry>> LoadLeaderboardAsync(int limit = 10)
    {
        if (leaderboardRef == null)
        {
            return new List<LeaderboardEntry>();
        }

        try
        {
            Debug.Log($"[Leaderboard] 리더보드 불러오기 시도...");

            // timeMs 오름차순 하위 limit개 = 가장 빠른 기록들
            Query query = leaderboardRef.OrderByChild("timeMs").LimitToFirst(limit);
            DataSnapshot snapshot = await query.GetValueAsync();
            List<LeaderboardEntry> leaderboardList = ParseEntries(snapshot);

            Debug.Log($"[Leaderboard] 리더보드 불러오기 성공");
            return leaderboardList;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Leaderboard] 리더보드 불러오기 실패! {ex.Message}");
            return new List<LeaderboardEntry>();
        }
    }

    public List<LeaderboardEntry> ParseEntries(DataSnapshot snapshot)
    {
        List<LeaderboardEntry> list = new List<LeaderboardEntry>();

        if (snapshot.Exists)
        {
            foreach (DataSnapshot child in snapshot.Children)
            {
                list.Add(LeaderboardEntry.FromJson(child.GetRawJsonValue()));
            }
        }

        // 최단 시간이 위로
        list.Sort((a, b) => a.timeMs.CompareTo(b.timeMs));
        return list;
    }

    public void StartRealtimeListener(int limit = 10)
    {
        if (isListenerActive || leaderboardRef == null)
            return;

        Debug.Log("[Leaderboard] 실시간 리스너 시작");
        listenerQuery = leaderboardRef.OrderByChild("timeMs").LimitToFirst(limit);
        listenerQuery.ValueChanged += OnValueChanged;
        isListenerActive = true;
    }

    public void StopRealtimeListener()
    {
        if (isListenerActive && listenerQuery != null)
        {
            Debug.Log("[Leaderboard] 실시간 리스너 중지");
            listenerQuery.ValueChanged -= OnValueChanged;
            listenerQuery = null;
            isListenerActive = false;
        }
    }

    private void OnValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError($"[Leaderboard] 리스너 오류: {args.DatabaseError.Message}");
            return;
        }

        List<LeaderboardEntry> leaderboard = ParseEntries(args.Snapshot);
        DispatchUpdateAsync(leaderboard).Forget();
    }

    private async UniTaskVoid DispatchUpdateAsync(List<LeaderboardEntry> leaderboard)
    {
        await UniTask.SwitchToMainThread();
        OnLeaderboardUpdated?.Invoke(leaderboard);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        StopRealtimeListener();
    }
}
