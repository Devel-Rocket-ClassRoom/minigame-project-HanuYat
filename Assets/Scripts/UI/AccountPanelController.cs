using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메뉴의 Login 버튼으로 열리는 계정 패널.
// 익명 로그인 상태: 닉네임 변경 + 로그아웃. 로그아웃 시 로그인 게이트 재노출.
public class AccountPanelController : MonoBehaviour
{
    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private TextMeshProUGUI statusText;

    [SerializeField]
    private TMP_InputField nicknameInput;

    // STOVE 로그인 시 InputField 대신 표시. "STOVE Account: {nick}"
    [SerializeField]
    private TextMeshProUGUI stoveAccountText;

    [SerializeField]
    private Button changeNickButton;

    [SerializeField]
    private Button logoutButton;

    [SerializeField]
    private Button closeButton;

    // 익명 계정에만 표시. 이메일 연결 패널 진입.
    [SerializeField]
    private Button linkButton;

    [SerializeField]
    private EmailAuthPanelController emailAuthPanel;

    // 로그아웃 후 다시 띄울 로그인 게이트 루트.
    [SerializeField]
    private GameObject loginGateRoot;

    private void Awake()
    {
        if (changeNickButton != null)
            changeNickButton.onClick.AddListener(() => OnChangeNick().Forget());
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogout);
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        if (linkButton != null)
            linkButton.onClick.AddListener(OnLinkClicked);
    }

    public void OnLinkClicked()
    {
        if (emailAuthPanel != null)
            emailAuthPanel.OpenForLink();
    }

    // 메뉴 Login 버튼이 호출.
    public void OpenPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        Refresh();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Refresh()
    {
        var auth = AuthManager.Instance;
        bool logged = auth != null && auth.IsLoggedIn;
        bool anon = logged && auth.IsAnonymous;
        bool email = logged && auth.IsEmailLinked;
        bool stove = logged && !anon && !email; // 커스텀 토큰(STOVE)

        // STOVE만 라벨, 나머지(익명/이메일)는 닉 입력 가능.
        if (nicknameInput != null)
            nicknameInput.gameObject.SetActive(!stove);
        if (stoveAccountText != null)
            stoveAccountText.gameObject.SetActive(stove);
        // 연결 버튼은 익명일 때만.
        if (linkButton != null)
            linkButton.gameObject.SetActive(anon);

        string nick = !string.IsNullOrEmpty(auth != null ? auth.DisplayName : null)
            ? auth.DisplayName
            : NicknameStore.Get();

        if (stove)
        {
            if (stoveAccountText != null)
                stoveAccountText.text = $"STOVE Account: {nick}";
        }
        else if (nicknameInput != null)
        {
            nicknameInput.text = NicknameStore.Get();
        }

        if (statusText != null)
        {
            if (!logged)
                statusText.text = "로그인되지 않음\nNot signed in";
            else if (stove)
                statusText.text = $"STOVE 로그인 중 · {nick}\nSigned in (STOVE) · {nick}";
            else if (email)
                statusText.text = $"이메일 계정 · {auth.Email}\nEmail account · {auth.Email}";
            else
                statusText.text =
                    $"익명 로그인 중 · {NicknameStore.Get()}\nSigned in (anonymous) · {NicknameStore.Get()}";
        }
    }

    private async UniTaskVoid OnChangeNick()
    {
        string nick = nicknameInput != null ? nicknameInput.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(nick))
        {
            if (statusText != null)
                statusText.text = "닉네임을 입력하세요\nEnter a nickname";
            return;
        }

        // 변경 전과 완전히 동일하면 거부 (대소문자 구분).
        if (nick == NicknameStore.Get())
        {
            if (statusText != null)
                statusText.text = "변경 전과 동일한 닉네임입니다\nSame as current nickname";
            return;
        }

        if (UserManager.Instance == null)
        {
            if (statusText != null)
                statusText.text = "사용자 시스템 연결 안 됨\nUser system unavailable";
            return;
        }

        var (ok, err) = await UserManager.Instance.SetNicknameAsync(nick);
        if (!ok)
        {
            if (statusText != null)
                statusText.text = err;
            return;
        }

        if (statusText != null)
            statusText.text =
                $"닉네임 변경됨 · {NicknameStore.Get()}\nNickname changed · {NicknameStore.Get()}";
    }

    private void OnLogout()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.SignOut();

        ClosePanel();
        if (loginGateRoot != null)
            loginGateRoot.SetActive(true); // 게이트 재노출 → 재로그인 강제
    }
}
