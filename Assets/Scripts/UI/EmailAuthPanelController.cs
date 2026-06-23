using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 이메일/비번 패널. 두 용도:
//  - Login: 다른 기기에서 연결된 계정 복구 (게이트에서 진입).
//  - Link : 현재 익명 계정에 이메일 연결, uid 보존 (계정 패널에서 진입).
public class EmailAuthPanelController : MonoBehaviour
{
    private enum Mode
    {
        Login,
        Link,
    }

    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private TextMeshProUGUI titleText;

    [SerializeField]
    private TextMeshProUGUI statusText;

    [SerializeField]
    private TMP_InputField emailInput;

    [SerializeField]
    private TMP_InputField passwordInput;

    [SerializeField]
    private Button actionButton;

    [SerializeField]
    private TextMeshProUGUI actionLabel;

    [SerializeField]
    private Button closeButton;

    // STOVE 계정 연결 (현재 준비 중 스텁). Link 모드에서만 노출.
    [SerializeField]
    private Button stoveLinkButton;

    // 이메일 로그인 성공 시 닫을 로그인 게이트.
    [SerializeField]
    private GameObject loginGateRoot;

    // 연결 성공 시 갱신할 계정 패널.
    [SerializeField]
    private AccountPanelController accountPanel;

    private Mode mode;

    private void Awake()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(() => OnAction().Forget());
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
        if (stoveLinkButton != null)
            stoveLinkButton.onClick.AddListener(OnStoveLink);
    }

    private void OnStoveLink()
    {
        SetStatus("STOVE 연결은 준비 중입니다.\nSTOVE linking is coming soon.");
    }

    public void OpenForLogin()
    {
        mode = Mode.Login;
        if (titleText != null)
            titleText.text = "Email Login";
        if (actionLabel != null)
            actionLabel.text = "Sign In";
        Open();
    }

    public void OpenForLink()
    {
        mode = Mode.Link;
        if (titleText != null)
            titleText.text = "Link Account";
        if (actionLabel != null)
            actionLabel.text = "Link";
        Open();
    }

    private void Open()
    {
        // STOVE 연결 버튼은 Link 모드에서만.
        if (stoveLinkButton != null)
            stoveLinkButton.gameObject.SetActive(mode == Mode.Link);

        if (emailInput != null)
            emailInput.text = string.Empty;
        if (passwordInput != null)
            passwordInput.text = string.Empty;
        SetStatus(string.Empty);
        SetInteractable(true);
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private async UniTaskVoid OnAction()
    {
        string email = emailInput != null ? emailInput.text.Trim() : string.Empty;
        string pw = passwordInput != null ? passwordInput.text : string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pw))
        {
            SetStatus("이메일/비밀번호를 입력하세요\nEnter email and password");
            return;
        }
        if (AuthManager.Instance == null)
        {
            SetStatus("인증 시스템 연결 안 됨\nAuth unavailable");
            return;
        }

        SetInteractable(false);
        SetStatus("처리 중...\nProcessing...");

        if (mode == Mode.Login)
        {
            var (ok, err) = await AuthManager.Instance.SignInUserWithEmailAsync(email, pw);
            if (!ok)
            {
                SetStatus($"{err}");
                SetInteractable(true);
                return;
            }
            // 서버 닉 복원 + 이메일 동기화 후 게이트 통과.
            if (UserManager.Instance != null)
            {
                await UserManager.Instance.LoadNicknameFromServerAsync();
                await UserManager.Instance.SyncEmailAsync();
            }
            ClosePanel();
            if (loginGateRoot != null)
                loginGateRoot.SetActive(false);
        }
        else
        {
            var (ok, err) = await AuthManager.Instance.LinkWithEmailAsync(email, pw);
            if (!ok)
            {
                SetStatus($"{err}");
                SetInteractable(true);
                return;
            }
            // 연결된 이메일을 users 노드에 동기화.
            if (UserManager.Instance != null)
                await UserManager.Instance.SyncEmailAsync();

            SetStatus("계정이 연결되었습니다\nAccount linked");
            if (accountPanel != null)
                accountPanel.OpenPanel(); // 상태 갱신
            ClosePanel();
        }
    }

    private void SetInteractable(bool v)
    {
        if (actionButton != null)
            actionButton.interactable = v;
        if (emailInput != null)
            emailInput.interactable = v;
        if (passwordInput != null)
            passwordInput.interactable = v;
    }

    private void SetStatus(string s)
    {
        if (statusText != null)
            statusText.text = s;
    }
}
