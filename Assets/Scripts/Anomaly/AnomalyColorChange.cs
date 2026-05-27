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
    private bool ready;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning("[AnomalyColorChange] targetRenderer 미할당 — 비활성.", this);
            return;
        }

        Material srcMat = targetRenderer.sharedMaterial;
        if (srcMat == null)
        {
            Debug.LogWarning(
                "[AnomalyColorChange] targetRenderer.sharedMaterial 없음 — 비활성.",
                this
            );
            return;
        }

        if (srcMat.HasProperty(BaseColorId))
            colorPropertyId = BaseColorId;
        else if (srcMat.HasProperty(ColorId))
            colorPropertyId = ColorId;
        else
        {
            Debug.LogWarning(
                $"[AnomalyColorChange] '{srcMat.name}' 에 _BaseColor / _Color 둘 다 없음 — 비활성.",
                this
            );
            return;
        }

        mpb = new MaterialPropertyBlock();
        originalColor = srcMat.GetColor(colorPropertyId);
        ready = true;
    }

    public override void Activate()
    {
        if (!ready)
            return;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(colorPropertyId, anomalyColor);
        targetRenderer.SetPropertyBlock(mpb);
        Debug.Log("[Anomaly] A03 AnomalyColorChange activated");
    }

    public override void Deactivate()
    {
        if (!ready)
            return;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(colorPropertyId, originalColor);
        targetRenderer.SetPropertyBlock(mpb);
    }
}
