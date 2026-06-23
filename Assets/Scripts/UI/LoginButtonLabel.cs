using TMPro;
using UnityEngine;

// 메뉴 Login 버튼 라벨. 로그인되면 닉네임(STOVE/익명), 아니면 기본 텍스트.
public class LoginButtonLabel : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI label;

    [SerializeField]
    private string loggedOutText = "LOGIN";

    private void Start()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.LoginStateChanged += OnLoginStateChanged;
        NicknameStore.OnChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.LoginStateChanged -= OnLoginStateChanged;
        NicknameStore.OnChanged -= Refresh;
    }

    private void OnLoginStateChanged(bool signedIn)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (label == null)
            return;

        bool logged = AuthManager.Instance != null && AuthManager.Instance.IsLogedIn;

        string display = null;
        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.DisplayName))
            display = AuthManager.Instance.DisplayName; // STOVE 등
        else if (NicknameStore.HasNickname)
            display = NicknameStore.Get();

        label.text = logged && display != null ? display : loggedOutText;
    }
}
