using UnityEngine;

public class AnomalyGhost : AnomalyEffectBase
{
    [SerializeField]
    private GameObject ghost;

    [SerializeField]
    private ClassroomLightSwitch lightSwitch;

    [SerializeField]
    private GhostChaser chaser;

    private Renderer[] ghostRenderers;
    private bool isArmed;

    private void Awake()
    {
        // ghost 하위 모든 Renderer 자동 수집 (Axe, 본체, 추가 의상 등 전부 포함)
        if (ghost != null)
            ghostRenderers = ghost.GetComponentsInChildren<Renderer>(true);

        // 씬에서 실수로 활성 상태로 뒀을 때를 대비한 안전 초기화
        if (ghost != null)
            ghost.SetActive(false);
        SetRenderersEnabled(false);
    }

    private void OnEnable()
    {
        if (lightSwitch != null)
            lightSwitch.LightStateChanged += OnLightStateChanged;
    }

    private void OnDisable()
    {
        if (lightSwitch != null)
            lightSwitch.LightStateChanged -= OnLightStateChanged;
    }

    public override void Activate()
    {
        isArmed = true;
        if (ghost != null)
            ghost.SetActive(true);
        if (chaser != null)
            chaser.SetArmed(true);
        UpdateVisibility();
        Debug.Log("[Anomaly] A12 AnomalyGhost activated");
    }

    public override void Deactivate()
    {
        isArmed = false;
        if (chaser != null)
            chaser.SetArmed(false);
        if (ghost != null)
            ghost.SetActive(false);
        SetRenderersEnabled(false);
    }

    public void ForceVisible()
    {
        SetRenderersEnabled(true);
    }

    private void OnLightStateChanged(bool on)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (!isArmed || lightSwitch == null)
            return;
        SetRenderersEnabled(!lightSwitch.IsOn);
    }

    private void SetRenderersEnabled(bool visible)
    {
        if (ghostRenderers == null)
            return;
        foreach (Renderer r in ghostRenderers)
        {
            if (r != null)
                r.forceRenderingOff = !visible;
        }
    }
}
