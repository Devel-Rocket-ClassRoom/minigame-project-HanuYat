using System.Collections;
using UnityEngine;

public class SlidingDoor : MonoBehaviour, IInteractable, IResettable
{
    // 실제로 움직일 문 오브젝트. 비워두면 이 GameObject 자신이 움직임
    [SerializeField]
    private Transform doorToMove;

    // 부모 로컬 좌표 기준 열림 방향/거리
    [SerializeField]
    private Vector3 openOffset = new Vector3(1.2f, 0f, 0f);

    [SerializeField]
    private float slideDuration = 0.35f;

    // 교실 문일 경우: 첫 오픈 시 책상 위치/불 켜기 힌트 1회 표시.
    [SerializeField]
    private bool showClassroomHintOnFirstOpen = false;

    [Header("SFX (선택)")]
    // 문 위치에서 재생할 3D AudioSource (SFX 믹서 그룹 라우팅). 비우면 무음.
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip openClip;

    [SerializeField]
    private AudioClip closeClip;

    // 앞/뒤 문 어느 쪽으로 들어와도 1회만 뜨도록 공유.
    private static bool classroomHintShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        classroomHintShown = false;
    }

    private Transform Door => doorToMove != null ? doorToMove : transform;

    private Vector3 closedPos;
    private bool isOpen;
    private Coroutine slideCoroutine;

    private void Awake()
    {
        closedPos = Door.localPosition;
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
        bool opening = !isOpen;

        // 닫힘 → 열림(첫 오픈) 시점에 교실 힌트 1회.
        if (showClassroomHintOnFirstOpen && !classroomHintShown && opening)
        {
            classroomHintShown = true;
            HintMessage.Instance?.ShowClassroom();
        }

        // SFX는 플레이어 상호작용 시점에만. Slide/ResetToDefault에 두면
        // 복도 리셋(매 텔레포트)마다 닫힘음이 울린다.
        PlaySfx(opening ? openClip : closeClip);

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(Slide(opening));
    }

    private void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void ResetToDefault()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
        Door.localPosition = closedPos;
        isOpen = false;
    }

    private IEnumerator Slide(bool opening)
    {
        isOpen = opening;

        Vector3 start = Door.localPosition;
        Vector3 target = opening ? closedPos + openOffset : closedPos;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            Door.localPosition = Vector3.Lerp(
                start,
                target,
                Mathf.SmoothStep(0f, 1f, elapsed / slideDuration)
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        Door.localPosition = target;
        slideCoroutine = null;
    }
}
