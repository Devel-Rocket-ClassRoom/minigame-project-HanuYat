using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

// 사용자 프로필(users/{uid}) + 닉네임 유니크 예약(nicknames/{nickname}).
// 닉네임: 문자(영/한/한자 등) + 숫자만, 특수문자/공백 불가. 대소문자 구분 유니크(Michael ≠ michael).
public class UserManager : MonoBehaviour
{
    private static UserManager instance;
    public static UserManager Instance => instance;

    private DatabaseReference usersRef;
    private DatabaseReference nicknamesRef;

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
            Debug.LogError("[User] 파이어베이스 초기화 실패!");
            return;
        }
        DatabaseReference root = FirebaseInitializer.Instance.Database.RootReference;
        usersRef = root.Child("users");
        nicknamesRef = root.Child("nicknames");
    }

    // 문자(유니코드 letter) + 숫자만, 1~MaxLength자, 특수문자/공백 불가.
    public static bool IsValidNickname(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;
        if (s.Length < 1 || s.Length > NicknameStore.MaxLength)
            return false;
        foreach (char c in s)
        {
            if (!char.IsLetterOrDigit(c))
                return false;
        }
        return true;
    }

    // 닉네임 설정/변경 + users 등록 + 유니크 예약. 로그인 직후·닉 변경 공용.
    public async UniTask<(bool ok, string error)> SetNicknameAsync(string nickname)
    {
        if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
            return (false, "로그인이 필요합니다.\nSign-in required.");
        if (usersRef == null || nicknamesRef == null)
            return (false, "DB 연결 안 됨\nDatabase unavailable");

        nickname = nickname == null ? string.Empty : nickname.Trim();
        if (!IsValidNickname(nickname))
            return (
                false,
                "문자/숫자 1~6자, 특수문자 불가\nLetters/digits only, 1-6 chars, no symbols"
            );

        string uid = AuthManager.Instance.UserId;
        // 대소문자 구분 유니크 → 닉 원본을 그대로 예약 키로 사용.
        string key = nickname;

        try
        {
            DataSnapshot userSnap = await usersRef.Child(uid).GetValueAsync();
            bool isNew = !userSnap.Exists;
            string prevKey = userSnap.HasChild("nickname")
                ? userSnap.Child("nickname").Value.ToString()
                : null;

            // 동일 닉(완전 일치) → 표시용 + lastLogin만 갱신, 예약 그대로.
            if (prevKey == key)
            {
                await usersRef
                    .Child(uid)
                    .UpdateChildrenAsync(
                        new Dictionary<string, object>
                        {
                            { "nickname", nickname },
                            { "lastLogin", ServerValue.Timestamp },
                        }
                    );
                NicknameStore.Set(nickname);
                await PropagateAsync(nickname);
                return (true, null);
            }

            // 새 키 트랜잭션 예약 — 남이 점유 중이면 Abort.
            bool reserved = false;
            try
            {
                await nicknamesRef
                    .Child(key)
                    .RunTransaction(md =>
                    {
                        if (md.Value != null && md.Value.ToString() != uid)
                        {
                            reserved = false;
                            return TransactionResult.Abort();
                        }
                        md.Value = uid;
                        reserved = true;
                        return TransactionResult.Success(md);
                    });
            }
            catch
            {
                reserved = false;
            }

            if (!reserved)
                return (false, "이미 사용 중인 닉네임입니다.\nNickname already taken.");

            try
            {
                var data = new Dictionary<string, object>
                {
                    { "nickname", nickname },
                    { "lastLogin", ServerValue.Timestamp },
                };
                if (isNew)
                    data["joinedAt"] = ServerValue.Timestamp;
                await usersRef.Child(uid).UpdateChildrenAsync(data);

                // 이전 닉 키 해제.
                if (!string.IsNullOrEmpty(prevKey))
                    await nicknamesRef.Child(prevKey).RemoveValueAsync();

                NicknameStore.Set(nickname);
                await PropagateAsync(nickname);
                return (true, null);
            }
            catch (Exception ex)
            {
                // 예약은 성공했는데 users 등록/후속이 실패한 경우 → 방금 예약한 새 키를 해제.
                // 안 그러면 삭제될 익명 uid가 닉을 영구 점유(orphan 락)해 아무도 못 가져감.
                try
                {
                    await nicknamesRef.Child(key).RemoveValueAsync();
                }
                catch
                {
                    /* 해제 실패는 무시 — best effort */
                }
                Debug.LogError($"[User] 닉네임 설정 실패(예약 롤백): {ex.Message}");
                return (false, $"오류: {ex.Message}\nError: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[User] 닉네임 설정 실패: {ex.Message}");
            return (false, $"오류: {ex.Message}\nError: {ex.Message}");
        }
    }

    // 서버 users/{uid}/nickname → 로컬 NicknameStore 복원 (다른 기기 이메일 로그인 후).
    public async UniTask LoadNicknameFromServerAsync()
    {
        if (usersRef == null || AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
            return;
        try
        {
            DataSnapshot snap = await usersRef
                .Child(AuthManager.Instance.UserId)
                .Child("nickname")
                .GetValueAsync();
            if (snap.Exists && snap.Value != null)
                NicknameStore.Set(snap.Value.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[User] 닉네임 로드 실패: {ex.Message}");
        }
    }

    // 이메일을 users/{uid}/email에 동기화 (링크/이메일 로그인 후). 비번은 저장 안 함.
    public async UniTask SyncEmailAsync()
    {
        if (usersRef == null || AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
            return;
        string email = AuthManager.Instance.Email;
        if (string.IsNullOrEmpty(email))
            return;
        try
        {
            await usersRef.Child(AuthManager.Instance.UserId).Child("email").SetValueAsync(email);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[User] 이메일 동기화 실패: {ex.Message}");
        }
    }

    // 리더보드 기록 있으면 닉 동기화.
    private async UniTask PropagateAsync(string nickname)
    {
        if (LeaderboardManager.Instance != null)
            await LeaderboardManager.Instance.UpdateNicknameAsync(nickname);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
