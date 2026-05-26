using UnityEngine;

public class AnomalyMaterialSwap : AnomalyEffectBase
{
    [SerializeField]
    private Renderer[] targetRenderers;

    [SerializeField]
    private Material anomalyMaterial;

    private Material[] originalMaterials;

    private void Awake()
    {
        originalMaterials = new Material[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
                continue;
            originalMaterials[i] = targetRenderers[i].sharedMaterial;
        }
    }

    public override void Activate()
    {
        if (anomalyMaterial == null)
            return;
        foreach (Renderer r in targetRenderers)
        {
            if (r == null)
                continue;
            r.material = anomalyMaterial;
        }
        Debug.Log("[Anomaly] A08 AnomalyMaterialSwap activated");
    }

    public override void Deactivate()
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null || originalMaterials[i] == null)
                continue;
            targetRenderers[i].material = originalMaterials[i];
        }
    }
}
