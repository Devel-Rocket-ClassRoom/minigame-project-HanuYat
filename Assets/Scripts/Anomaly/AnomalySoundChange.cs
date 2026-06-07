using UnityEngine;

/// <summary>
/// A07 청각 이상현상 — 평소 환경음(Ambience) 클립을 이상 클립으로 교체.
/// "비가 안 오는데 빗소리가 들리는" 부조화를 노린다. 2D 전역음 유지.
/// Deactivate() 시 원래 환경음으로 복원.
/// </summary>
public class AnomalySoundChange : AnomalyEffectBase
{
    [SerializeField]
    private AudioSource ambientSource;

    [SerializeField]
    private AudioClip anomalyClip;

    private AudioClip originalClip;
    private bool swapped;
    private bool ready;

    private void Awake()
    {
        if (ambientSource == null)
        {
            Debug.LogWarning("[AnomalySoundChange] ambientSource 미할당 — 비활성.", this);
            return;
        }
        if (anomalyClip == null)
        {
            Debug.LogWarning("[AnomalySoundChange] anomalyClip 미할당 — 비활성.", this);
            return;
        }

        originalClip = ambientSource.clip;
        ready = true;
    }

    public override void Activate()
    {
        if (!ready || ambientSource == null)
            return;

        // 클립 교체만으론 재생 중인 소스가 안 바뀜 — Play() 재호출 필요.
        ambientSource.clip = anomalyClip;
        ambientSource.Play();
        swapped = true;
        AnomalyLog.Activated("A07 AnomalySoundChange");
    }

    public override void Deactivate()
    {
        if (!ready || !swapped || ambientSource == null)
            return;

        ambientSource.clip = originalClip;
        ambientSource.Play();
        swapped = false;
    }
}
