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

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private bool isOn;

    // 첫 적용(Awake) 전까지 isOn 기본값과 무관하게 강제 적용 보장 — 멱등 가드가 초기화를 막지 않도록.
    private bool initialized;

    public bool IsOn => isOn;

    public event System.Action<bool> LightStateChanged;

    private void Awake()
    {
        ApplyState(startOn);
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
}
