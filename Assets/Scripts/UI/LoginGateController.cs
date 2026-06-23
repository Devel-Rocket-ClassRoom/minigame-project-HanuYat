using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인메뉴 진입 전 로그인 게이트. 로그인 + 닉네임 완료해야 메뉴 사용 가능.
// 이미 로그인 + 닉네임 있으면 자동 스킵.
public class LoginGateController : MonoBehaviour
{
    // 게이트 비주얼 루트(캔버스). 닫을 때 비활성화.
    [SerializeField]
    private GameObject gateRoot;

    [SerializeField]
    private TMP_InputField nicknameInput;

    [SerializeField]
    private Button anonStartButton;

    // STOVE 로그인 (현재 준비 중 스텁).
    [SerializeField]
    private Button stoveButton;

    // 이메일 로그인 (연결한 계정 복구).
    [SerializeField]
    private Button emailLoginButton;

    [SerializeField]
    private EmailAuthPanelController emailAuthPanel;

    [SerializeField]
    private TextMeshProUGUI statusText;

    private async UniTaskVoid Start()
    {
        if (anonStartButton != null)
            anonStartButton.onClick.AddListener(() => OnAnonStart().Forget());
        if (stoveButton != null)
            stoveButton.onClick.AddListener(OnStove);
        if (emailLoginButton != null)
            emailLoginButton.onClick.AddListener(OnEmailLogin);

        ShowGate();
        SetStatus("초기화 중...\nInitializing...");

        bool ready =
            FirebaseInitializer.Instance != null
            && await FirebaseInitializer.Instance.WaitForInitializationAsync();
        await UniTask.WaitUntil(() =>
            AuthManager.Instance != null && AuthManager.Instance.IsInitialized
        );

        // 명시적 로그아웃 상태면 복원된 세션을 정리하고 게이트 유지.
        if (ready && AuthManager.Instance.WasSignedOut && AuthManager.Instance.IsLogedIn)
            AuthManager.Instance.SignOut();

        // 자동 스킵: 이미 로그인 + 닉네임 있음.
        if (ready && AuthManager.Instance.IsLogedIn && NicknameStore.HasNickname)
        {
            CloseGate();
            return;
        }

        if (nicknameInput != null)
            nicknameInput.text = NicknameStore.HasNickname ? NicknameStore.Get() : string.Empty;
        SetStatus(
            ready
                ? "닉네임을 입력하세요\nEnter a nickname"
                : "오프라인 — 연결을 확인하세요\nOffline — check your connection"
        );
    }

    // 게이트가 다시 켜질 때(로그아웃 등) UI를 초기 상태로 리셋.
    // 첫 활성화 땐 직후 Start가 상태를 덮어쓰므로 무해.
    private void OnEnable()
    {
        SetInteractable(true);
        if (nicknameInput != null)
            nicknameInput.text = NicknameStore.HasNickname ? NicknameStore.Get() : string.Empty;
        SetStatus("닉네임을 입력하세요\nEnter a nickname");
    }

    private void ShowGate()
    {
        if (gateRoot != null)
            gateRoot.SetActive(true);
    }

    private void CloseGate()
    {
        if (gateRoot != null)
            gateRoot.SetActive(false);
    }

    private async UniTaskVoid OnAnonStart()
    {
        string nick = nicknameInput != null ? nicknameInput.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(nick))
        {
            SetStatus("닉네임을 입력하세요\nEnter a nickname");
            return;
        }

        SetInteractable(false);
        SetStatus("로그인 중...\nSigning in...");

        bool freshSignIn = false;
        if (!AuthManager.Instance.IsLogedIn)
        {
            var (ok, err) = await AuthManager.Instance.SignInAnnonymouslyAsync();
            if (!ok)
            {
                SetStatus(err);
                SetInteractable(true);
                return;
            }
            freshSignIn = true;
        }

        // 닉네임 등록 + 유니크 예약 + users 등록. 중복/형식 오류면 게이트 유지.
        if (UserManager.Instance == null)
        {
            if (freshSignIn)
                await AuthManager.Instance.DeleteCurrentUserAsync();
            SetStatus("사용자 시스템 연결 안 됨\nUser system unavailable");
            SetInteractable(true);
            return;
        }
        var (uok, uerr) = await UserManager.Instance.SetNicknameAsync(nick);
        if (!uok)
        {
            // 방금 만든 익명 계정이면 롤백 — 닉 미확정 상태로 로그인 유지 방지.
            if (freshSignIn)
                await AuthManager.Instance.DeleteCurrentUserAsync();
            SetStatus(uerr);
            SetInteractable(true);
            return;
        }

        CloseGate();
    }

    private void OnStove()
    {
        SetStatus("STOVE 로그인은 준비 중입니다.\nSTOVE login is coming soon.");
    }

    private void OnEmailLogin()
    {
        if (emailAuthPanel != null)
            emailAuthPanel.OpenForLogin();
    }

    private void SetInteractable(bool value)
    {
        if (anonStartButton != null)
            anonStartButton.interactable = value;
        if (nicknameInput != null)
            nicknameInput.interactable = value;
    }

    private void SetStatus(string s)
    {
        if (statusText != null)
            statusText.text = s;
    }
}
