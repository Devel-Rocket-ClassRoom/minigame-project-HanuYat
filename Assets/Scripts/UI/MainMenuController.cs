using System.Collections;
using Michsky.UI.Dark;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private string gameSceneName = "Game Scene";

    [SerializeField]
    private GameObject settingsPanel;

    [SerializeField]
    private GameObject leaderboardPanel;

    [SerializeField]
    private FadeController fadeController;

    // 스타트 → 페이드아웃 후 검은 화면에 띄우는 스토리 모달(Dark UI). 클릭/아무키로 게임 씬 진입.
    [SerializeField]
    private ModalWindowManager storyModal;

    // 스토리 표시 후 입력 폴링 시작까지의 짧은 지연(스타트 클릭이 스킵으로 새는 것 방지).
    [SerializeField]
    private float inputUnlockDelay = 0.4f;

    // 키 입력 후 스토리 모달 아웃 연출 시간 — 그 뒤 게임 씬 로드(검은 화면 유지).
    [SerializeField]
    private float storyOutDelay = 0.5f;

    private bool isStarting;
    private bool storyShowing;
    private bool loading;
    private float inputUnlockTime;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        // 스토리 모달은 스타트 전까지 숨김 — 에디터에서 켜둔 채 시작해도 강제 비활성.
        if (storyModal != null && storyModal.gameObject.activeSelf)
            storyModal.gameObject.SetActive(false);
    }

    public void OnStartClicked()
    {
        if (isStarting)
            return;
        isStarting = true;

        // 스토리 모달이 있으면: 페이드아웃 → 스토리 표시 → 클릭/아무키로 로드.
        if (storyModal != null && fadeController != null)
            fadeController.FadeOut(ShowStory);
        else if (fadeController != null)
            fadeController.StartTransition(() => SceneManager.LoadScene(gameSceneName));
        else
            SceneManager.LoadScene(gameSceneName);
    }

    private void ShowStory()
    {
        storyModal.ModalWindowIn();
        storyShowing = true;
        inputUnlockTime = Time.unscaledTime + inputUnlockDelay;
    }

    private void Update()
    {
        if (!storyShowing || loading || Time.unscaledTime < inputUnlockTime)
            return;

        bool pressed =
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (pressed)
        {
            loading = true;
            StartCoroutine(StoryToGame());
        }
    }

    // 스토리 모달을 코드로 직접 페이드아웃(CanvasGroup 알파) → 뒤의 메뉴 페이드(검은 화면) 노출 →
    // 게임 씬 로드. 게임 씬은 fadeInOnStart로 검은 화면에서 페이드인. (블랙 브릿지 보장)
    private IEnumerator StoryToGame()
    {
        CanvasGroup cg = storyModal != null ? storyModal.GetComponent<CanvasGroup>() : null;

        // Dark UI Animator가 CanvasGroup 알파를 잡고 있으면 lerp와 충돌 → 비활성.
        Animator anim = storyModal != null ? storyModal.GetComponent<Animator>() : null;
        if (anim != null)
            anim.enabled = false;

        float t = 0f;
        while (t < storyOutDelay)
        {
            if (cg != null)
                cg.alpha = Mathf.Lerp(1f, 0f, t / storyOutDelay);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (cg != null)
            cg.alpha = 0f;

        SceneManager.LoadScene(gameSceneName);
    }

    public void OnSettingsClicked()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OnLeaderboardClicked()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
