using System;
using UnityEngine;

// 매 턴 회수해야 하는 영어책. 회수 시 가시성/충돌을 끄고(파괴/SetActive 아님),
// 복도 전환 시 ResettableRegistry가 ResetToDefault를 호출해 다시 표시한다.
// SetActive(false)로 숨기면 OnDisable이 발화해 Unregister되어 ResetAll에서 누락 → 영구 소실(소프트락)되므로 금지.
public class BookPickup : MonoBehaviour, IInteractable, IResettable
{
    public static event Action OnCollected;
    public static event Action OnReset;

    [SerializeField]
    private Renderer bookRenderer;

    [SerializeField]
    private Collider bookCollider;

    // EPOOutline.Outlinable (선택) — 네임스페이스 의존을 피하려 Behaviour로 참조
    [SerializeField]
    private Behaviour outlinable;

    // 불이 켜져 있어야 책을 집을 수 있다. 어두우면 회수 차단.
    [SerializeField]
    private ClassroomLightSwitch lightSwitch;

    private bool collected;

    private void Awake()
    {
        if (bookRenderer == null)
            bookRenderer = GetComponent<Renderer>();
        if (bookCollider == null)
            bookCollider = GetComponent<Collider>();
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
        if (collected)
            return;

        // 불이 꺼져 있으면 어두워서 회수 불가 — 힌트만 표시.
        if (lightSwitch != null && !lightSwitch.IsOn)
        {
            HintMessage.Instance?.ShowDark();
            return;
        }

        SetCollected(true);
        OnCollected?.Invoke();
        HintMessage.Instance?.ShowPickup();
    }

    public void ResetToDefault()
    {
        SetCollected(false);
        OnReset?.Invoke();
    }

    private void SetCollected(bool value)
    {
        collected = value;
        ApplyVisible(!value);
    }

    private void ApplyVisible(bool visible)
    {
        if (bookRenderer != null)
            bookRenderer.enabled = visible;
        if (bookCollider != null)
            bookCollider.enabled = visible;
        if (outlinable != null)
            outlinable.enabled = visible;
    }
}
