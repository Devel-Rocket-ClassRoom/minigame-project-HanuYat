using UnityEngine;
using UnityEngine.Rendering;

// 물 잠김 비주얼/오디오 효과 통합 컨트롤.
// - 완전 잠김: Hovl 수중 파티클 + 언더워터 Volume(블루틴트/블러) + AudioLowPassFilter(먹먹)
// - 수위 상승 긴장: 별도 tension Volume(비네팅)을 수위 진행도에 비례해 가중
public class UnderwaterEffect : MonoBehaviour
{
    [Header("완전 잠김 효과")]
    [SerializeField]
    private GameObject underwaterParticles; // Hovl 파티클 4개 묶음 부모 (카메라 자식)

    [SerializeField]
    private Volume underwaterVolume; // 블루틴트 + DepthOfField 등

    [SerializeField]
    private AudioLowPassFilter lowPassFilter; // 카메라 AudioListener 옆

    [SerializeField]
    private float normalCutoff = 22000f;

    [SerializeField]
    private float underwaterCutoff = 900f;

    [Header("수위 상승 긴장 효과")]
    [SerializeField]
    private Volume tensionVolume; // 비네팅 등

    [Header("전환 속도")]
    [SerializeField]
    private float weightLerpSpeed = 4f;

    [SerializeField]
    private float cutoffLerpSpeed = 8f;

    private float targetUnderwaterWeight;
    private float targetTensionWeight;
    private float targetCutoff;

    private void Awake()
    {
        ResetEffect();
        ApplyImmediate();
    }

    // 완전 잠김 토글 — 파티클 + Volume + lowpass.
    public void SetUnderwater(bool active)
    {
        targetUnderwaterWeight = active ? 1f : 0f;
        targetCutoff = active ? underwaterCutoff : normalCutoff;
        if (underwaterParticles != null)
            underwaterParticles.SetActive(active);
        if (lowPassFilter != null)
            lowPassFilter.enabled = active;
    }

    // 수위 진행도(0~1)에 비례한 긴장 비네팅.
    public void SetRisingTension(float t)
    {
        targetTensionWeight = Mathf.Clamp01(t);
    }

    // 전부 끔 — Deactivate 시 호출.
    public void ResetEffect()
    {
        targetUnderwaterWeight = 0f;
        targetTensionWeight = 0f;
        targetCutoff = normalCutoff;
        if (underwaterParticles != null)
            underwaterParticles.SetActive(false);
        if (lowPassFilter != null)
            lowPassFilter.enabled = false;
    }

    private void ApplyImmediate()
    {
        if (underwaterVolume != null)
            underwaterVolume.weight = targetUnderwaterWeight;
        if (tensionVolume != null)
            tensionVolume.weight = targetTensionWeight;
        if (lowPassFilter != null)
            lowPassFilter.cutoffFrequency = targetCutoff;
    }

    private void Update()
    {
        if (underwaterVolume != null)
            underwaterVolume.weight = Mathf.MoveTowards(
                underwaterVolume.weight,
                targetUnderwaterWeight,
                weightLerpSpeed * Time.deltaTime
            );
        if (tensionVolume != null)
            tensionVolume.weight = Mathf.MoveTowards(
                tensionVolume.weight,
                targetTensionWeight,
                weightLerpSpeed * Time.deltaTime
            );
        if (lowPassFilter != null && lowPassFilter.enabled)
            lowPassFilter.cutoffFrequency = Mathf.MoveTowards(
                lowPassFilter.cutoffFrequency,
                targetCutoff,
                cutoffLerpSpeed * 3000f * Time.deltaTime
            );
    }
}
