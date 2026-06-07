using UnityEngine;
using UnityEngine.UI;

// 숨참기 게이지 — 플레이어 완전 잠김 동안만 표시. progress 1=가득, 0=소진.
// AnomalyRisingWater가 Show/Hide/SetProgress 호출.
public class BreathMeterUI : MonoBehaviour
{
    public static BreathMeterUI Instance { get; private set; }

    [SerializeField]
    private GameObject root; // 슬라이더 컨테이너 (켜고 끔)

    [SerializeField]
    private Slider slider; // value 0~1

    [SerializeField]
    private Image fillImage; // 선택 — 잔량 따라 색 변화

    [SerializeField]
    private Color fullColor = new Color(0.4f, 0.8f, 1f);

    [SerializeField]
    private Color lowColor = new Color(1f, 0.2f, 0.2f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
        SetProgress(1f);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    // progress: 1=숨 가득, 0=소진.
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (slider != null)
            slider.SetValueWithoutNotify(progress);
        if (fillImage != null)
            fillImage.color = Color.Lerp(lowColor, fullColor, progress);
    }
}
