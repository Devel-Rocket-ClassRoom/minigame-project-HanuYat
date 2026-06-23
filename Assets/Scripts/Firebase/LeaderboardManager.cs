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
        if (!AuthManager.Instance.IsLoggedIn)
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

    // 기존 기록이 있으면 그 닉네임만 갱신. 기록 없으면 무시(부분 노드 생성 방지).
    public async UniTask UpdateNicknameAsync(string nickname)
    {
        if (
            leaderboardRef == null
            || AuthManager.Instance == null
            || !AuthManager.Instance.IsLoggedIn
        )
            return;
        if (string.IsNullOrWhiteSpace(nickname))
            return;

        string uid = AuthManager.Instance.UserId;
        try
        {
            DataSnapshot exist = await leaderboardRef.Child(uid).Child("timeMs").GetValueAsync();
            if (!exist.Exists)
                return; // 기록 없음 → 닉만 쓰면 검증 실패하므로 스킵

            await leaderboardRef.Child(uid).Child("nickname").SetValueAsync(nickname);
            Debug.Log("[Leaderboard] 닉네임 갱신 완료");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Leaderboard] 닉네임 갱신 실패: {ex.Message}");
        }
    }

    // timeMs 오름차순(최단 상위) 상위 limit개 반환. limit <= 0 이면 전체.
    // 등수 = 리스트 인덱스 + 1 (상위 limit 안에 있을 때만 정확).
    public async UniTask<List<LeaderboardEntry>> LoadTopAsync(int limit = 100)
    {
        if (leaderboardRef == null)
        {
            return new List<LeaderboardEntry>();
        }

        try
        {
            Debug.Log($"[Leaderboard] 상위 불러오기 시도... (limit={limit})");

            // timeMs 인덱스 정렬 + 상위 N개만 (rules의 ".indexOn":"timeMs").
            Query query = leaderboardRef.OrderByChild("timeMs");
            if (limit > 0)
                query = query.LimitToFirst(limit);

            DataSnapshot snapshot = await query.GetValueAsync();
            List<LeaderboardEntry> leaderboardList = ParseEntries(snapshot);

            Debug.Log($"[Leaderboard] 상위 불러오기 성공 ({leaderboardList.Count}명)");
            return leaderboardList;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Leaderboard] 불러오기 실패! {ex.Message}");
            return new List<LeaderboardEntry>();
        }
    }

    // 내 단일 기록(leaderboard/{uid}) 직접 조회. 없으면 null. (상위 N 밖일 때 내 기록 표시용)
    public async UniTask<LeaderboardEntry> GetMyEntryAsync()
    {
        if (
            leaderboardRef == null
            || AuthManager.Instance == null
            || !AuthManager.Instance.IsLoggedIn
        )
            return null;

        try
        {
            DataSnapshot snap = await leaderboardRef
                .Child(AuthManager.Instance.UserId)
                .GetValueAsync();
            if (!snap.Exists)
                return null;
            return LeaderboardEntry.FromJson(snap.GetRawJsonValue());
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Leaderboard] 내 기록 조회 실패: {ex.Message}");
            return null;
        }
    }

    // 내 등수(전체 기준). timeMs <= 내기록 인 항목 수로 산정 → 동률은 같은 구간으로 묶인다.
    // ⚠️ RTDB는 서버측 count가 없어 해당 구간을 스캔한다. 상위 N 안에 있으면 이 호출 없이
    //    리스트 인덱스로 등수를 쓰는 게 싸다(LeaderboardUI 참고).
    public async UniTask<int> GetMyRankAsync(int myTimeMs)
    {
        if (leaderboardRef == null || myTimeMs <= 0)
            return -1;

        try
        {
            DataSnapshot snap = await leaderboardRef
                .OrderByChild("timeMs")
                .EndAt(myTimeMs)
                .GetValueAsync();
            return (int)snap.ChildrenCount;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Leaderboard] 등수 조회 실패: {ex.Message}");
            return -1;
        }
    }

    private List<LeaderboardEntry> ParseEntries(DataSnapshot snapshot)
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

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
