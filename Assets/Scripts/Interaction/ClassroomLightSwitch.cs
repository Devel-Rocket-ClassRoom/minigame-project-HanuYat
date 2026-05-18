using UnityEngine;

public class ClassroomLightSwitch : MonoBehaviour, IInteractable
{
    [SerializeField]
    private Light[] roomLights;

    [SerializeField]
    private Renderer[] ledRenderers;

    [SerializeField]
    private Color emissionOnColor = new Color(1f, 0.92f, 0.7f) * 2f;

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
            l.enabled = on;

        Color targetColor = on ? emissionOnColor : Color.black;
        foreach (Renderer r in ledRenderers)
        {
            r.material.SetColor(EmissionColorId, targetColor);
            r.material.EnableKeyword("_EMISSION");
        }

        isOn = on;
    }
}
