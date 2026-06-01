using UnityEngine;

// CanvasGroup 알파를 사인파로 펄스 → 텍스트 깜빡임. "아무 키나 눌러 시작" 같은 프롬프트용.
[RequireComponent(typeof(CanvasGroup))]
public class BlinkingText : MonoBehaviour
{
    [SerializeField]
    private float speed = 3f;

    [SerializeField]
    private float minAlpha = 0.15f;

    [SerializeField]
    private float maxAlpha = 1f;

    private CanvasGroup cg;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        cg.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
    }
}
