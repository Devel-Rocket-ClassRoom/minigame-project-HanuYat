using System;
using System.Collections;
using UnityEngine;

public class CorridorDoor : MonoBehaviour, IInteractable
{
    public enum DoorDirection
    {
        Forward,
        Backward,
    }

    public static event Action<DoorDirection> OnDoorUsed;
    public static event Action OnCorridorEntered;

    public static void RaiseDoorUsed(DoorDirection direction) => OnDoorUsed?.Invoke(direction);

    public static void RaiseCorridorEntered() => OnCorridorEntered?.Invoke();

    [SerializeField]
    private DoorDirection direction;

    [SerializeField]
    private FadeController fadeController;

    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private JudgementSystem judgement;

    [SerializeField]
    private ExitSequence exitSequence;

    [Header("SFX (선택)")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip openClip;

    [SerializeField]
    private AudioClip closeClip;

    [SerializeField]
    private AudioClip lockedClip;

    // -1이면 전체 클립 사용. 양수면 해당 초까지만 잘라서 런타임 AudioClip 생성.
    [SerializeField]
    private float lockedClipMaxDuration = 0.5f;

    // 페이드인을 사운드 종료 기준보다 얼마나 앞당길지(초). 문마다 개별 조정.
    [SerializeField]
    private float fadeInAdvance = 0.3f;

    private CharacterController characterController;
    private PlayerFootsteps footsteps;
    private bool inTransition;
    private bool pendingClear;

    private void Start()
    {
        if (playerController != null)
        {
            characterController = playerController.GetComponent<CharacterController>();
            footsteps = playerController.GetComponent<PlayerFootsteps>();
        }

        if (
            lockedClip != null
            && lockedClipMaxDuration > 0f
            && lockedClipMaxDuration < lockedClip.length
        )
            lockedClip = TrimClip(lockedClip, lockedClipMaxDuration);
    }

    public void Interact()
    {
        if (inTransition)
            return;
        if (fadeController == null || spawnPoint == null || playerController == null)
        {
            Debug.LogWarning(
                $"[CorridorDoor] {name}: 필수 참조가 비어있어 전환을 실행할 수 없습니다.",
                this
            );
            return;
        }

        // 첫 턴 후진 차단: 들어온 문(Backward)으로는 나갈 수 없다 — 출구로 전진해야 한다.
        // 책 게이트보다 먼저 → 후진 문은 책 회수 여부와 무관하게 항상 안내.
        if (direction == DoorDirection.Backward && judgement != null && judgement.IsFirstTurn)
        {
            PlaySfxOneShot(lockedClip);
            HintMessage.Instance?.ShowEntrance();
            return;
        }

        // 턴 목표 게이트: 책 미회수 시 전환 차단(힌트 표시).
        // inTransition/OnDoorUsed/pendingClear 세팅 전에 return → JudgementSystem 카운터 desync 없음.
        if (TurnObjective.Instance != null && !TurnObjective.Instance.IsBookCollected)
        {
            PlaySfxOneShot(lockedClip);
            HintMessage.Instance?.ShowLocked();
            return;
        }

        // 전역 전환 락 — 다른 시퀀스(귀신/새 피격) 전환 진행 중이면 진입 차단(소프트락 방지).
        // hint 게이트 통과 후, 상태 세팅/OnDoorUsed 전에 획득 → 실패 시 카운터 desync 없음.
        if (!fadeController.TryLockTransition())
            return;

        pendingClear = judgement != null && judgement.WouldClearOnDoorUse(direction);
        inTransition = true;
        OnDoorUsed?.Invoke(direction);
        playerController.enabled = false;
        if (footsteps != null)
            footsteps.enabled = false;

        float openLen = openClip != null ? openClip.length : 0f;
        float closeLen = closeClip != null ? closeClip.length : 0f;
        float holdNeeded =
            audioSource != null
                ? Mathf.Max(0.1f, openLen + closeLen - fadeController.FadeDuration - fadeInAdvance)
                : -1f;

        if (audioSource != null && (openClip != null || closeClip != null))
            StartCoroutine(DoorSfxSequence(openLen));

        fadeController.StartTransition(OnMidpoint, OnComplete, holdNeeded);
    }

    private void OnMidpoint()
    {
        if (!pendingClear)
        {
            CharacterController cc = characterController;
            bool ccEnabled = cc != null && cc.enabled;
            if (cc != null)
                cc.enabled = false;

            playerController.transform.SetPositionAndRotation(
                spawnPoint.position,
                Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f)
            );

            if (cc != null)
                cc.enabled = ccEnabled;

            playerController.ResetLook();

            ResettableRegistry.ResetAll();

            AnomalyManager.Instance?.Refresh();
        }
        else
        {
            exitSequence?.TriggerClear();
        }
    }

    private void OnComplete()
    {
        if (!pendingClear)
        {
            playerController.enabled = true;
            if (footsteps != null)
                footsteps.enabled = true;
            fadeController.UnlockTransition();
            OnCorridorEntered?.Invoke();
        }
        // pendingClear(클리어) 경로는 엔딩 종단 — 락 유지로 추가 전환 차단.
        inTransition = false;
    }

    private IEnumerator DoorSfxSequence(float openLen)
    {
        PlaySfxOneShot(openClip);
        yield return new WaitForSeconds(openLen);
        PlaySfxOneShot(closeClip);
    }

    private void PlaySfxOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private static AudioClip TrimClip(AudioClip source, float duration)
    {
        int samples = Mathf.Min((int)(duration * source.frequency), source.samples);
        float[] data = new float[source.samples * source.channels];
        source.GetData(data, 0);
        float[] trimmed = new float[samples * source.channels];
        System.Array.Copy(data, 0, trimmed, 0, trimmed.Length);
        AudioClip clip = AudioClip.Create(
            source.name + "_trim",
            samples,
            source.channels,
            source.frequency,
            false
        );
        clip.SetData(trimmed, 0);
        return clip;
    }
}
