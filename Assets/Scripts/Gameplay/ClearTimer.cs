using Cysharp.Threading.Tasks;
using UnityEngine;

// 클리어 타이머.
// 시작: 튜토리얼 종이 등장(TutorialPaperReveal.OnRevealed).
// 정지: 클리어 확정(ExitSequence.OnCleared) → 경과 시간 리더보드 제출.
// ESC 일시정지(timeScale=0)에도 계속 흐르도록 Time.unscaledTime 사용.
public class ClearTimer : MonoBehaviour
{
    // 마지막 클리어 시간(ms). 미클리어 시 -1. UI 등에서 참조.
    public static int LastClearMs { get; private set; } = -1;

    // 마지막 제출이 신기록(기존 기록 갱신/첫 기록)이었는지.
    public static bool LastWasNewRecord { get; private set; }

    // 마지막 제출 처리 완료 여부 (클리어 화면이 신기록 판정 대기용).
    public static bool SubmitDone { get; private set; }

    private float startUnscaled = -1f;
    private bool running;

    private void OnEnable()
    {
        TutorialPaperReveal.OnRevealed += StartTimer;
        ExitSequence.OnCleared += StopAndSubmit;
    }

    private void OnDisable()
    {
        TutorialPaperReveal.OnRevealed -= StartTimer;
        ExitSequence.OnCleared -= StopAndSubmit;
    }

    private void StartTimer()
    {
        if (running)
            return;

        startUnscaled = Time.unscaledTime;
        running = true;
        Debug.Log("[ClearTimer] 시작");
    }

    private void StopAndSubmit()
    {
        if (!running)
            return;

        running = false;
        int elapsedMs = Mathf.RoundToInt((Time.unscaledTime - startUnscaled) * 1000f);
        LastClearMs = elapsedMs;
        LastWasNewRecord = false;
        SubmitDone = false;
        Debug.Log($"[ClearTimer] 정지 — {elapsedMs}ms ({TimeUtil.ToClearTimeString(elapsedMs)})");

        SubmitAsync(elapsedMs).Forget();
    }

    private async UniTaskVoid SubmitAsync(int ms)
    {
        try
        {
            if (LeaderboardManager.Instance == null)
            {
                Debug.LogWarning("[ClearTimer] LeaderboardManager 없음 — 제출 스킵");
                return;
            }

            // 익명 로그인 보장 (AuthManager 초기화 대기 후 미로그인 시 로그인).
            if (AuthManager.Instance != null)
            {
                await UniTask.WaitUntil(() => AuthManager.Instance.IsInitialized);
                if (!AuthManager.Instance.IsLogedIn)
                {
                    var (ok, authError) = await AuthManager.Instance.SignInAnnonymouslyAsync();
                    if (!ok)
                    {
                        Debug.LogWarning($"[ClearTimer] 익명 로그인 실패 — 제출 스킵: {authError}");
                        return;
                    }
                }
            }

            var (success, saved, error) = await LeaderboardManager.Instance.SubmitTimeAsync(ms);
            if (!success)
            {
                Debug.LogWarning($"[ClearTimer] 제출 실패: {error}");
                return;
            }

            LastWasNewRecord = saved;
            Debug.Log(saved ? "[ClearTimer] 신기록 저장" : "[ClearTimer] 기존 기록이 더 빠름 — 갱신 안 함");
        }
        finally
        {
            SubmitDone = true;
        }
    }
}
