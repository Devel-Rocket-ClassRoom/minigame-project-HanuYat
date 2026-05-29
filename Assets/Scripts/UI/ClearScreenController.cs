using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClearScreenController : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private Button restartButton;

    [SerializeField]
    private Button mainMenuButton;

    [SerializeField]
    private string mainMenuSceneName = "MainMenu Scene";

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        restartButton?.onClick.AddListener(OnRestart);
        mainMenuButton?.onClick.AddListener(OnMainMenu);
    }

    public void Show()
    {
        // Canvas 루트가 비활성이면 패널만 켜도 activeInHierarchy=false라 렌더되지 않는다.
        // 루트를 먼저 활성화하면 첫 활성화 시 Awake가 실행되어 패널 숨김 + 버튼 리스너 등록이 보장된다.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (panel != null)
            panel.SetActive(true);
    }

    private void OnRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
