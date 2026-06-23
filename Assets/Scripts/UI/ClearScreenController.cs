using System.Collections;
using TMPro;
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

    // 자막을 먼저 보여주고 버튼은 이만큼 뒤에 노출(감정 종결 강조).
    [SerializeField]
    private float buttonRevealDelay = 1.5f;

    // 클리어 타임 표기 (자막과 버튼 사이).
    [SerializeField]
    private TextMeshProUGUI clearTimeText;

    // 신기록 시 표시할 오브젝트(BlinkingText로 일렁임). 평소 비활성.
    [SerializeField]
    private GameObject newRecordObject;

    // 신기록 판정(제출 완료) 대기 상한.
    [SerializeField]
    private float newRecordWaitTimeout = 2f;

    [SerializeField]
    private string mainMenuSceneName = "MainMenu Scene";

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        // 버튼은 자막 노출 후 지연 등장 — 시작 시 숨김.
        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);

        if (newRecordObject != null)
            newRecordObject.SetActive(false);

        restartButton?.onClick.AddListener(OnRestart);
        mainMenuButton?.onClick.AddListener(OnMainMenu);
    }

    private void OnDestroy()
    {
        restartButton?.onClick.RemoveListener(OnRestart);
        mainMenuButton?.onClick.RemoveListener(OnMainMenu);
    }

    public void Show()
    {
        // Canvas 루트가 비활성이면 패널만 켜도 activeInHierarchy=false라 렌더되지 않는다.
        // 루트를 먼저 활성화하면 첫 활성화 시 Awake가 실행되어 패널 숨김 + 버튼 리스너 등록이 보장된다.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (panel != null)
            panel.SetActive(true);

        // 클리어 타임 표기 (자막과 함께 노출).
        if (clearTimeText != null)
        {
            clearTimeText.gameObject.SetActive(true);
            clearTimeText.text =
                ClearTimer.LastClearMs > 0
                    ? $"Clear Time: {TimeUtil.ToClearTimeString(ClearTimer.LastClearMs)}"
                    : "Clear Time: --:--.---";
        }

        if (newRecordObject != null)
            newRecordObject.SetActive(false);

        // 자막/타임 먼저 → (신기록이면 New Record) → 버튼 나중.
        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        // 자막/타임 보여준 뒤 버튼 등장까지 대기.
        yield return new WaitForSecondsRealtime(buttonRevealDelay);

        // 신기록 판정(제출 완료) 대기 — 상한 내. (보통 이미 완료)
        float waited = 0f;
        while (!ClearTimer.SubmitDone && waited < newRecordWaitTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        // New Record와 버튼을 함께 노출.
        if (newRecordObject != null && ClearTimer.LastWasNewRecord)
            newRecordObject.SetActive(true); // BlinkingText가 일렁임

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(true);
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
