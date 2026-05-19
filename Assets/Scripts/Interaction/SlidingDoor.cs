using System.Collections;
using UnityEngine;

public class SlidingDoor : MonoBehaviour, IInteractable
{
    // 실제로 움직일 문 오브젝트. 비워두면 이 GameObject 자신이 움직임
    [SerializeField]
    private Transform doorToMove;

    // 부모 로컬 좌표 기준 열림 방향/거리
    [SerializeField]
    private Vector3 openOffset = new Vector3(1.2f, 0f, 0f);

    [SerializeField]
    private float slideDuration = 0.35f;

    private Transform Door => doorToMove != null ? doorToMove : transform;

    private Vector3 closedPos;
    private bool isOpen;
    private Coroutine slideCoroutine;

    private void Awake()
    {
        closedPos = Door.localPosition;
    }

    public void Interact()
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(Slide(!isOpen));
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
