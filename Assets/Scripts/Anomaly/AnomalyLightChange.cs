using UnityEngine;

public class AnomalyLightChange : AnomalyEffectBase
{
    [SerializeField]
    private Light[] targetLights;

    [SerializeField]
    private Color anomalyColor = Color.red;

    [SerializeField]
    private float anomalyIntensity = 0.3f;

    private Color[] originalColors;
    private float[] originalIntensities;

    private void Awake()
    {
        originalColors = new Color[targetLights.Length];
        originalIntensities = new float[targetLights.Length];
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] == null)
                continue;
            originalColors[i] = targetLights[i].color;
            originalIntensities[i] = targetLights[i].intensity;
        }
    }

    public override void Activate()
    {
        foreach (Light l in targetLights)
        {
            if (l == null)
                continue;
            l.color = anomalyColor;
            l.intensity = anomalyIntensity;
        }
        Debug.Log("[Anomaly] A04 AnomalyLightChange activated");
    }

    public override void Deactivate()
    {
        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] == null)
                continue;
            targetLights[i].color = originalColors[i];
            targetLights[i].intensity = originalIntensities[i];
        }
    }
}
