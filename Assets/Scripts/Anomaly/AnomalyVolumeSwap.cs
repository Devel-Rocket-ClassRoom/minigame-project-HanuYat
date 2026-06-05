using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// A10 시점 왜곡: 씬의 Global Volume 프로파일 참조를 이상현상 전용 프로파일로 교체한다.
// sharedProfile 의 method 호출/override 값 직접 변형은 금지 (PR#40 형 asset 영구 변형).
// 여기서는 참조(reference)만 스왑하므로 원본 프로파일 asset 은 보존된다.
// anomalyProfile 은 Vignette / Lens Distortion / Film Grain 이 설정된 전용 asset.
//
// 추가로 활성 중 간헐적으로 TV 노이즈 버스트(글리치):
//  - glitchImage(풀스크린 RawImage)에 런타임 생성 노이즈 텍스처를 깔고 uvRect 를 매 프레임
//    랜덤 이동시켜 지지직 정전기처럼 표시 (post-processing FilmGrain 은 어두운 씬에서 약해서 오버레이 사용).
//  - glitchVolume weight 를 함께 깜빡여 ChromaticAberration 색번짐 보강 (asset 변형 없음).
//  - 치-익 노이즈 사운드 재생.
public class AnomalyVolumeSwap : AnomalyEffectBase
{
    [SerializeField]
    private Volume targetVolume;

    [SerializeField]
    private VolumeProfile anomalyProfile;

    [Header("Glitch burst")]
    [SerializeField]
    private RawImage glitchImage; // 풀스크린 노이즈 오버레이 (평소 비활성)

    [SerializeField]
    private Volume glitchVolume; // 전용 글리치 프로파일 Volume (평소 weight 0) — CA 보강용, 선택

    [SerializeField]
    private AudioSource glitchAudio; // 치-익 노이즈

    [SerializeField]
    private float burstIntervalMin = 5f;

    [SerializeField]
    private float burstIntervalMax = 12f;

    [SerializeField]
    private float burstDuration = 0.45f;

    [SerializeField]
    private float noiseAlpha = 0.95f; // 오버레이 최대 불투명도 (낮으면 씬이 비쳐 색번짐)

    [SerializeField]
    private float noiseTiling = 4f; // uvRect 타일 — 클수록 입자 곱게

    private VolumeProfile originalProfile;
    private Texture2D noiseTex;
    private bool glitching;
    private float burstTimer;
    private Coroutine burstCo;

    private void Awake()
    {
        if (targetVolume != null)
            originalProfile = targetVolume.sharedProfile;
        if (glitchVolume != null)
            glitchVolume.weight = 0f;
        if (glitchImage != null)
        {
            noiseTex = BuildNoiseTexture(256);
            glitchImage.texture = noiseTex;
            glitchImage.enabled = false;
        }
    }

    public override void Activate()
    {
        if (targetVolume == null || anomalyProfile == null)
            return;
        targetVolume.sharedProfile = anomalyProfile;

        glitching = true;
        burstTimer = Random.Range(burstIntervalMin, burstIntervalMax);
        if (glitchVolume != null)
            glitchVolume.weight = 0f;
        Debug.Log("[Anomaly] A10 AnomalyVolumeSwap activated");
    }

    public override void Deactivate()
    {
        StopGlitch();
        if (targetVolume == null || originalProfile == null)
            return;
        targetVolume.sharedProfile = originalProfile;
    }

    private void Update()
    {
        if (!glitching)
            return;
        burstTimer -= Time.deltaTime;
        if (burstTimer <= 0f)
        {
            burstTimer = Random.Range(burstIntervalMin, burstIntervalMax);
            if (burstCo != null)
                StopCoroutine(burstCo);
            burstCo = StartCoroutine(GlitchBurst());
        }
    }

    private IEnumerator GlitchBurst()
    {
        if (glitchAudio != null && glitchAudio.clip != null)
            glitchAudio.Play();
        if (glitchImage != null)
            glitchImage.enabled = true;

        float t = 0f;
        while (t < burstDuration)
        {
            t += Time.deltaTime;
            float n = t / burstDuration;
            // 앞 70% 는 꽉 찬 static, 끝 30% 만 페이드아웃 (중간에 씬 비치는 색번짐 방지)
            float holdFade = n < 0.7f ? 1f : 1f - (n - 0.7f) / 0.3f;
            float flicker = 0.8f + 0.2f * Mathf.Abs(Mathf.Sin(t * 45f));

            if (glitchImage != null)
            {
                // 노이즈 텍스처를 랜덤 이동시켜 정전기 애니메이션
                glitchImage.uvRect = new Rect(Random.value, Random.value, noiseTiling, noiseTiling);
                Color c = glitchImage.color;
                c.a = Mathf.Clamp01(holdFade * flicker * noiseAlpha);
                glitchImage.color = c;
            }
            if (glitchVolume != null)
                glitchVolume.weight = Mathf.Clamp01(holdFade * flicker);
            yield return null;
        }

        EndBurstVisuals();
        burstCo = null;
    }

    private void EndBurstVisuals()
    {
        if (glitchVolume != null)
            glitchVolume.weight = 0f;
        if (glitchImage != null)
            glitchImage.enabled = false;
    }

    private void StopGlitch()
    {
        glitching = false;
        if (burstCo != null)
        {
            StopCoroutine(burstCo);
            burstCo = null;
        }
        EndBurstVisuals();
        if (glitchAudio != null && glitchAudio.isPlaying)
            glitchAudio.Stop();
    }

    // 흑백 화이트노이즈 텍스처 (Repeat/Point) — uvRect 이동으로 정전기 애니메이션.
    private Texture2D BuildNoiseTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point,
        };
        var px = new Color32[size * size];
        for (int i = 0; i < px.Length; i++)
        {
            // 흑 아니면 백 — 쨍한 TV 정전기 (회색 단계 줄임)
            byte v = (byte)(Random.value < 0.5f ? 0 : 255);
            px[i] = new Color32(v, v, v, 255);
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
