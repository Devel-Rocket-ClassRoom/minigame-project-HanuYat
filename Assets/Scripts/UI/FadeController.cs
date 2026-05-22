using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    [SerializeField]
    private Image fadeImage;

    [SerializeField]
    private float fadeDuration = 1f;

    [SerializeField]
    private float midpointHold = 0.1f;

    private Coroutine transitionCoroutine;

    private void Awake()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }
    }

    public void StartTransition(Action onMidpoint, Action onComplete = null)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionRoutine(onMidpoint, onComplete));
    }

    private IEnumerator TransitionRoutine(Action onMidpoint, Action onComplete)
    {
        yield return Fade(0f, 1f);
        onMidpoint?.Invoke();

        if (midpointHold > 0f)
            yield return new WaitForSecondsRealtime(midpointHold);

        yield return Fade(1f, 0f);

        transitionCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null)
            yield break;

        float elapsed = 0f;
        Color c = fadeImage.color;
        while (elapsed < fadeDuration)
        {
            c.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = c;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
    }
}
