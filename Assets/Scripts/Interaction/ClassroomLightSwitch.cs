using UnityEngine;

public class ClassroomLightSwitch : MonoBehaviour, IInteractable
{
    [SerializeField]
    private Light[] roomLights;

    [SerializeField]
    private float lightIntensityOn = 40f;

    [SerializeField]
    private Renderer[] ledRenderers;

    [SerializeField]
    private Color emissionOnColor = new Color(1f, 0.95f, 0.8f) * 6f;

    [SerializeField]
    private bool startOn = true;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private bool isOn;

    private void Awake()
    {
        ApplyState(startOn);
    }

    public void Interact()
    {
        ApplyState(!isOn);
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
    }
}
