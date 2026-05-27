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
