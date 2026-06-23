using System;
using UnityEngine;
using Firebase.Auth;
using Cysharp.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    private static AuthManager instance;
    public static AuthManager Instance => instance;

    private FirebaseAuth auth;
    private FirebaseUser currentUser;

    private bool isInitialized = false;
    private bool lastNotifiedSingendIn = false;

    public FirebaseUser CurrentUser => currentUser;
    public bool IsLogedIn => currentUser != null;
    public string UserId => currentUser?.UserId ?? string.Empty;
    public bool IsInitialized => isInitialized;

    // 익명 로그인 여부. STOVE 등 커스텀 토큰 로그인 시 false.
    public bool IsAnonymous => currentUser != null && currentUser.IsAnonymous;

    // 계정 표시 이름(STOVE 닉 등). 익명은 보통 비어 있음.
    public string DisplayName => currentUser != null ? currentUser.DisplayName : string.Empty;

    public string Email => currentUser != null ? currentUser.Email : string.Empty;

    // 이메일/비번 자격증명이 연결돼 있는지 (provider "password").
    public bool IsEmailLinked
    {
        get
        {
            if (currentUser == null)
                return false;
            foreach (var p in currentUser.ProviderData)
            {
                if (p.ProviderId == "password")
                    return true;
            }
            return false;
        }
    }

    // 명시적 로그아웃 유지 플래그 — 재실행 시 복원 세션 자동 로그인 방지.
    private const string SignedOutKey = "auth_signed_out";
    public bool WasSignedOut => PlayerPrefs.GetInt(SignedOutKey, 0) == 1;

    private void SetSignedOut(bool v)
    {
        PlayerPrefs.SetInt(SignedOutKey, v ? 1 : 0);
        PlayerPrefs.Save();
    }

    public event Action<bool> LoginStateChanged;

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
        bool isReady = await FirebaseInitializer.Instance.WaitForInitializationAsync();
        if (!isReady)
        {
            Debug.LogError("[Auth] 파이어 베이스 초기화 실패! Auth 초기화 불가...");
            return;
        }

        auth = FirebaseInitializer.Instance.Auth;
        auth.StateChanged += OnAuthStateChanged;

        currentUser = auth.CurrentUser;
        Debug.Log(currentUser != null ? "[Auth] 이미 로그인 됨" : "[Auth] 로그인 필요");

        isInitialized = true;
        NotifyLoginState();
    }

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        NotifyLoginState();
    }

    private void NotifyLoginState()
    {
        bool signedIn = IsLogedIn;
        if (signedIn == lastNotifiedSingendIn)
            return;

        lastNotifiedSingendIn = signedIn;
        Debug.Log(signedIn ? $"[Auth] 로그인 상태: {UserId}" : "[Auth] 로그아웃 상태");
        LoginStateChanged?.Invoke(signedIn);
    }

    public async UniTask<(bool success, string error)> SignInAnnonymouslyAsync()
    {
        try
        {
            Debug.Log("[Auth] 익명 로그인 시도...");

            AuthResult result = await auth.SignInAnonymouslyAsync();
            currentUser = result.User;
            SetSignedOut(false);
            NotifyLoginState();

            Debug.Log($"[Auth] 익명 로그인 성공: {currentUser.UserId}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"[Auth] 익명 로그인 실패: {ex.Message}");
            return (false, ParseFirebaseError(ex.Message));
        }
    }

    public async UniTask<(bool success, string error)> SignInUserWithEmailAsync(string email, string password)
    {
        try
        {
            Debug.Log("[Auth] 이메일 로그인 시도...");

            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            currentUser = result.User;
            SetSignedOut(false);

            Debug.Log($"[Auth] 이메일 로그인 성공: {currentUser.UserId}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"[Auth] 이메일 로그인 실패: {ex.Message}");
            return (false, ParseFirebaseError(ex.Message));
        }
    }

    // 현재(익명) 계정에 이메일/비번 연결 — uid 보존 → 기록 유지.
    public async UniTask<(bool success, string error)> LinkWithEmailAsync(
        string email,
        string password
    )
    {
        if (currentUser == null)
            return (false, "로그인 상태가 아닙니다.\nNot signed in.");

        try
        {
            Debug.Log("[Auth] 이메일 계정 연결 시도...");
            Credential cred = EmailAuthProvider.GetCredential(email, password);
            AuthResult result = await currentUser.LinkWithCredentialAsync(cred);
            currentUser = result.User;
            SetSignedOut(false);
            NotifyLoginState();

            Debug.Log($"[Auth] 계정 연결 성공: {currentUser.Email}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"[Auth] 계정 연결 실패: {ex.Message}");
            return (false, ParseFirebaseError(ex.Message));
        }
    }

    // 현재 계정 삭제 (닉 예약 실패한 신규 익명 롤백용). 삭제 실패 시 로그아웃으로 폴백.
    public async UniTask<(bool ok, string error)> DeleteCurrentUserAsync()
    {
        if (currentUser == null)
            return (true, null);
        try
        {
            await currentUser.DeleteAsync();
            currentUser = null;
            SetSignedOut(true);
            NotifyLoginState();
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Auth] 계정 삭제 실패 — 로그아웃 폴백: {ex.Message}");
            SignOut();
            return (false, ex.Message);
        }
    }

    public void SignOut()
    {
        SetSignedOut(true); // 명시적 로그아웃 — 재실행 시 자동 로그인 방지
        if (auth != null && currentUser != null)
        {
            Debug.Log("[Auth] 로그아웃");
            auth.SignOut();
            currentUser = null;
            NotifyLoginState();
        }
    }

    private string ParseFirebaseError(string error)
    {
        Debug.LogWarning($"[Auth] Firebase 에러 원문: {error}");

        string lower = error.ToLowerInvariant();

        if (lower.Contains("already in use") || lower.Contains("email-already"))
        {
            return "이미 사용 중인 이메일입니다.\nEmail already in use.";
        }
        if (lower.Contains("at least 6") || lower.Contains("weak") || lower.Contains("password is invalid"))
        {
            return "비밀번호는 6자 이상이어야 합니다.\nPassword must be at least 6 characters.";
        }
        if (lower.Contains("badly formatted") || lower.Contains("invalid-email"))
        {
            return "이메일 형식이 올바르지 않습니다.\nInvalid email format.";
        }
        if (lower.Contains("network"))
        {
            return "네트워크 연결을 확인해주세요.\nCheck your network connection.";
        }
        if (
            lower.Contains("operation-not-allowed")
            || lower.Contains("operation not allowed")
            || lower.Contains("admin-restricted")
            || lower.Contains("configuration")
            || lower.Contains("not enabled")
        )
        {
            return "이메일 로그인이 비활성화됨 — Firebase 콘솔에서 활성화 필요.\nEmail sign-in disabled — enable it in Firebase Console.";
        }
        if (lower.Contains("already linked") || lower.Contains("provider-already-linked"))
        {
            return "이미 연결된 계정입니다.\nAccount already linked.";
        }

        return "이메일 또는 비밀번호를 확인해주세요.\nCheck your email or password.";
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (auth != null)
        {
            auth.StateChanged -= OnAuthStateChanged;
        }
    }
}
