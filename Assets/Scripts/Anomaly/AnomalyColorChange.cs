using UnityEngine;

public class AnomalyColorChange : AnomalyEffectBase
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField]
    private Renderer targetRenderer;

    [SerializeField]
    private Color anomalyColor = Color.red;

    private MaterialPropertyBlock mpb;
    private Color originalColor;
    private int colorPropertyId;

    private void Awake()
    {
        if (targetRenderer == null)
            return;

        mpb = new MaterialPropertyBlock();
        Material srcMat = targetRenderer.sharedMaterial;

        colorPropertyId = srcMat.HasProperty(BaseColorId) ? BaseColorId : ColorId;
        originalColor = srcMat.HasProperty(colorPropertyId)
            ? srcMat.GetColor(colorPropertyId)
            : Color.white;
    }

    public override void Activate()
    {
        if (targetRenderer == null)
            return;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(colorPropertyId, anomalyColor);
        targetRenderer.SetPropertyBlock(mpb);
        Debug.Log("[Anomaly] A03 AnomalyColorChange activated");
    }

    public override void Deactivate()
    {
        if (targetRenderer == null)
            return;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(colorPropertyId, originalColor);
        targetRenderer.SetPropertyBlock(mpb);
    }
}
