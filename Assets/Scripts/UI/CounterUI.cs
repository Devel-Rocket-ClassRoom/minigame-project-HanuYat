using System.Collections;
using TMPro;
using UnityEngine;

public class CounterUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text counterText;

    [SerializeField]
    private AudioSource updateSfx;

    [SerializeField]
    private int goal = 8;

    [Header("Pulse")]
    [SerializeField]
    private float pulseDuration = 0.2f;

    [SerializeField]
    private float pulseScale = 1.2f;

    private int current;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        Render();
    }

    public void UpdateCounter(int newValue)
    {
        current = Mathf.Max(0, newValue);
        Render();

        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(Pulse());

        if (updateSfx != null)
            updateSfx.Play();
    }

    private void Render()
    {
        if (counterText != null)
            counterText.text = $"{current} / {goal}";
    }

    private IEnumerator Pulse()
    {
        if (counterText == null)
            yield break;

        RectTransform rt = counterText.rectTransform;
        float half = pulseDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            rt.localScale = Vector3.one * Mathf.Lerp(1f, pulseScale, t / half);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            rt.localScale = Vector3.one * Mathf.Lerp(pulseScale, 1f, t / half);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        rt.localScale = Vector3.one;
        pulseCoroutine = null;
    }

    [ContextMenu("Test +1")]
    private void TestIncrement() => UpdateCounter(current + 1);

    [ContextMenu("Test Reset")]
    private void TestReset() => UpdateCounter(0);
}
