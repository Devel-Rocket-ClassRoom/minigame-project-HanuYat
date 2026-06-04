using UnityEngine;

public class ClassroomLightSwitch : MonoBehaviour, IInteractable, IResettable
{
    [SerializeField]
    private Light[] roomLights;

    [SerializeField]
    private float lightIntensityOn = 5f;

    [SerializeField]
    private Renderer[] ledRenderers;

    [SerializeField]
    private Color emissionOnColor = Color.white * 4f;

    [SerializeField]
    private bool startOn = true;

    [Header("SFX (선택)")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip switchClip;

    [SerializeField]
    private float switchClipStartOffset = 0f;

    [SerializeField]
    private float switchClipMaxDuration = 1f;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private bool isOn;

    // 첫 적용(Awake) 전까지 isOn 기본값과 무관하게 강제 적용 보장 — 멱등 가드가 초기화를 막지 않도록.
    private bool initialized;

    public bool IsOn => isOn;

    public event System.Action<bool> LightStateChanged;

    private void Awake()
    {
        ApplyState(startOn);

        if (
            switchClip != null
            && (switchClipStartOffset > 0f || switchClipMaxDuration < switchClip.length)
        )
            switchClip = TrimClip(switchClip, switchClipStartOffset, switchClipMaxDuration);
    }

    private void OnEnable()
    {
        ResettableRegistry.Register(this);
    }

    private void OnDisable()
    {
        ResettableRegistry.Unregister(this);
    }

    public void Interact()
    {
        if (audioSource != null && switchClip != null)
            audioSource.PlayOneShot(switchClip);
        ApplyState(!isOn);
    }

    public void ResetToDefault()
    {
        ApplyState(startOn);
    }

    private void ApplyState(bool on)
    {
        // 멱등 가드 — 상태 동일 시 머티리얼 재설정/이벤트 재발행 스킵(매 텔레포트 리셋 시 불필요 작업 방지).
        if (initialized && on == isOn)
            return;
        initialized = true;

        foreach (Light l in roomLights)
        {
            l.enabled = on;
            l.intensity = on ? lightIntensityOn : 0f;
        }

        Color ledColor = on ? emissionOnColor : Color.black;
        foreach (Renderer r in ledRenderers)
        {
            r.material.SetColor(EmissionColorId, ledColor);
            r.material.EnableKeyword("_EMISSION");
        }

        isOn = on;

        LightStateChanged?.Invoke(on);
    }

    private static AudioClip TrimClip(AudioClip source, float startTime, float endTime)
    {
        int startSample = Mathf.Clamp((int)(startTime * source.frequency), 0, source.samples);
        int endSample = Mathf.Clamp((int)(endTime * source.frequency), startSample, source.samples);
        int samples = endSample - startSample;
        float[] data = new float[source.samples * source.channels];
        source.GetData(data, 0);
        float[] trimmed = new float[samples * source.channels];
        System.Array.Copy(data, startSample * source.channels, trimmed, 0, trimmed.Length);
        AudioClip clip = AudioClip.Create(
            source.name + "_trim",
            samples,
            source.channels,
            source.frequency,
            false
        );
        clip.SetData(trimmed, 0);
        return clip;
    }
}
