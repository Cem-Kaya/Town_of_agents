using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerFacing2D : MonoBehaviour
{
    [Header("Facing sprites")]
    public Sprite downSprite;
    public Sprite upSprite;
    public Sprite rightSprite;
    [Tooltip("Leave empty to mirror Right for left-facing.")]
    public Sprite leftSprite;

    [Header("Idle")]
    [Tooltip("Return to Down after this many seconds without a move.")]
    public float idleReturnDelay = 1.2f;

    private SpriteRenderer sr;
    private float lastMoveTime;
    private Facing current = Facing.Down;

    private enum Facing { Down, Up, Left, Right }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ApplyFacing(Facing.Down, force: true);
        lastMoveTime = Time.time;
    }

    void Update()
    {
        // idle snap-back
        if (Time.time - lastMoveTime >= idleReturnDelay && current != Facing.Down)
            ApplyFacing(Facing.Down);
    }

    /// Call this when a grid move starts; delta is (-1,0,1) on each axis.
    public void NotifyMoveDelta(Vector3Int deltaCell)
    {
        if (deltaCell == Vector3Int.zero) return;
        lastMoveTime = Time.time;

        Facing f;
        if (Mathf.Abs(deltaCell.x) >= Mathf.Abs(deltaCell.y))
            f = deltaCell.x >= 0 ? Facing.Right : Facing.Left;
        else
            f = deltaCell.y >= 0 ? Facing.Up : Facing.Down;

        ApplyFacing(f);
    }

    private void ApplyFacing(Facing f, bool force = false)
    {
        if (!force && f == current) return;

        switch (f)
        {
            case Facing.Down: SetSprite(downSprite, false); break;
            case Facing.Up: SetSprite(upSprite, false); break;
            case Facing.Right: SetSprite(rightSprite, false); break;
            case Facing.Left:
                if (leftSprite) SetSprite(leftSprite, false);
                else SetSprite(rightSprite, true); // mirror right
                break;
        }
        current = f;
    }

    private void SetSprite(Sprite s, bool flipX)
    {
        if (s) sr.sprite = s;
        sr.flipX = flipX;
    }
}
