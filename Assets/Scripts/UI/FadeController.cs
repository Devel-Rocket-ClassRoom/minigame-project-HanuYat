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

    // 씬 로드 시 검은 화면에서 시작해 페이드인 — 인터스티셜→게임 씬 진입 연출용.
    [SerializeField]
    private bool fadeInOnStart = false;

    private Coroutine transitionCoroutine;

    // 시퀀스 전역 전환 락 — CorridorDoor/Ghost/Bird가 같은 FadeController를 공유하므로,
    // 한 시퀀스 진행 중(페이드 前 카메라 연출 포함) 다른 시퀀스가 StartTransition으로
    // 첫 코루틴을 죽여 OnComplete 미실행 → inTransition 고착(소프트락)되는 것을 방지.
    private bool transitionLocked;

    // 진행 중이 아니면 락 획득 후 true, 이미 잠겨 있으면 false.
    public bool TryLockTransition()
    {
        if (transitionLocked)
            return false;
        transitionLocked = true;
        return true;
    }

    public void UnlockTransition() => transitionLocked = false;

    private void Awake()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = fadeInOnStart ? 1f : 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = fadeInOnStart;
        }
    }

    private void Start()
    {
        if (fadeInOnStart)
        {
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(FadeInRoutine());
        }
    }

    private IEnumerator FadeInRoutine()
    {
        yield return Fade(1f, 0f);
        if (fadeImage != null)
            fadeImage.raycastTarget = false;
        transitionCoroutine = null;
    }

    public float FadeDuration => fadeDuration;

    // holdOverride < 0 이면 Inspector midpointHold 사용.
    public void StartTransition(
        Action onMidpoint,
        Action onComplete = null,
        float holdOverride = -1f
    )
    {
        float hold = holdOverride >= 0f ? holdOverride : midpointHold;
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionRoutine(onMidpoint, onComplete, hold));
    }

    // 페이드 아웃(0→1)만 하고 검은 화면 유지 — 인터스티셜(스토리 창)용.
    public void FadeOut(Action onComplete = null)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(FadeOutRoutine(onComplete));
    }

    private IEnumerator FadeOutRoutine(Action onComplete)
    {
        if (fadeImage != null)
            fadeImage.raycastTarget = true;
        yield return Fade(0f, 1f);
        transitionCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator TransitionRoutine(Action onMidpoint, Action onComplete, float hold)
    {
        yield return Fade(0f, 1f);

        if (hold > 0f)
            yield return new WaitForSecondsRealtime(hold);

        onMidpoint?.Invoke();

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
