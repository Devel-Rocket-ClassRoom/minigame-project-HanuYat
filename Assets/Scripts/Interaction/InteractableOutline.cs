using EPOOutline;
using UnityEngine;

[RequireComponent(typeof(Outlinable))]
public class InteractableOutline : MonoBehaviour
{
    [Header("Default (idle)")]
    [SerializeField]
    private bool showDefaultOutline = true;

    [SerializeField]
    private Color defaultColor = Color.white;

    [SerializeField, Range(0f, 1f)]
    private float defaultAlpha = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float defaultDilate = 0.15f;

    [SerializeField, Range(0f, 1f)]
    private float defaultBlur = 0.5f;

    [Header("Hover")]
    [SerializeField]
    private Color hoverColor = Color.tomato;

    [SerializeField, Range(0f, 1f)]
    private float hoverAlpha = 1f;

    [SerializeField, Range(0f, 1f)]
    private float hoverDilate = 0.4f;

    [SerializeField, Range(0f, 1f)]
    private float hoverBlur = 0.1f;

    private Outlinable outlinable;

    private void Awake()
    {
        outlinable = GetComponent<Outlinable>();
        // FrontBack 모드 — 벽 등에 가려진 부분은 BackParameters로 그려지는데
        // BackParameters.Enabled = false로 두면 가려진 부분은 그려지지 않음.
        outlinable.RenderStyle = RenderStyle.FrontBack;
        outlinable.BackParameters.Enabled = false;
        ApplyDefault();
    }

    public void SetHovered(bool hovered)
    {
        if (hovered)
        {
            ApplyHover();
        }
        else
        {
            ApplyDefault();
        }
    }

    private void ApplyDefault()
    {
        if (!showDefaultOutline)
        {
            outlinable.FrontParameters.Enabled = false;
            return;
        }

        outlinable.FrontParameters.Enabled = true;
        outlinable.FrontParameters.Color = ColorWithAlpha(defaultColor, defaultAlpha);
        outlinable.FrontParameters.DilateShift = defaultDilate;
        outlinable.FrontParameters.BlurShift = defaultBlur;
    }

    private void ApplyHover()
    {
        outlinable.FrontParameters.Enabled = true;
        outlinable.FrontParameters.Color = ColorWithAlpha(hoverColor, hoverAlpha);
        outlinable.FrontParameters.DilateShift = hoverDilate;
        outlinable.FrontParameters.BlurShift = hoverBlur;
    }

    private static Color ColorWithAlpha(Color rgb, float a)
    {
        rgb.a = a;
        return rgb;
    }
}
