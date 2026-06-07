using UnityEngine;

public class AnomalyDeskRotation : AnomalyEffectBase
{
    [SerializeField]
    private Transform studentDesksParent;

    [SerializeField]
    private Transform book;

    [SerializeField]
    private Transform bookParentDesk;

    private Transform[] children;
    private Quaternion[] originalRotations;
    private Transform originalBookParent;

    private void Awake()
    {
        if (studentDesksParent == null)
        {
            children = new Transform[0];
            originalRotations = new Quaternion[0];
            return;
        }

        int count = studentDesksParent.childCount;
        children = new Transform[count];
        originalRotations = new Quaternion[count];
        for (int i = 0; i < count; i++)
        {
            children[i] = studentDesksParent.GetChild(i);
            originalRotations[i] = children[i].localRotation;
        }

        if (book != null)
            originalBookParent = book.parent;
    }

    public override void Activate()
    {
        if (book != null && bookParentDesk != null)
            book.SetParent(bookParentDesk, true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null)
                continue;
            children[i].localRotation = originalRotations[i] * Quaternion.Euler(0f, -90f, 0f);
        }

        // move 했으면(bookParentDesk 존재) 반드시 원위치 복원 — originalBookParent가 null(루트)이어도 대칭 유지.
        if (book != null && bookParentDesk != null)
            book.SetParent(originalBookParent, true);

        AnomalyLog.Activated("DeskRotation");
    }

    public override void Deactivate()
    {
        if (book != null && bookParentDesk != null)
            book.SetParent(bookParentDesk, true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null)
                continue;
            children[i].localRotation = originalRotations[i];
        }

        // move 했으면(bookParentDesk 존재) 반드시 원위치 복원 — originalBookParent가 null(루트)이어도 대칭 유지.
        if (book != null && bookParentDesk != null)
            book.SetParent(originalBookParent, true);
    }
}
