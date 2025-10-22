using System.Collections;
using UnityEngine;

/// Grid step movement with collision using your collider's shape (Box/Capsule/Circle).
/// Unity 6. Add to Player (Rigidbody2D + ANY 2D collider).
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Scene References")]
    public Grid grid;

    [Header("Movement")]
    [Min(0.01f)] public float moveTime = 0.12f;
    [Range(0f, 0.2f)] public float skin = 0.04f;

    [Header("Collision")]
    public LayerMask obstacleMask;
    public bool debugGizmos = false;

    // NEW: allow disabling input
    [HideInInspector] public bool inputEnabled = true;

    Rigidbody2D rb;
    Collider2D col;
    bool isMoving;

    // gizmo cache
    Vector3 gizmoCenter;
    Vector2 gizmoSize;
    CapsuleDirection2D gizmoCapsuleDir;
    enum GizmoShape { Box, Capsule, Circle }
    GizmoShape gizmoShape;

    PlayerFacing2D facing;   

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (!col) Debug.LogError("Player: add a 2D collider (Box/Capsule/Circle).");

        if (!grid)
        {
            grid = FindAnyObjectByType<Grid>();
            if (!grid) Debug.LogError("Player: no Grid found/assigned.");
        }

        SnapToGridCenter();
        facing = GetComponent<PlayerFacing2D>();   // add this

    }

    public void SetInputEnabled(bool on)
    {
        inputEnabled = on;
        isMoving = false; // safety
    }

    void Update()
    {
        if (!inputEnabled) return;
        if (isMoving) return;

        int x = (int)Input.GetAxisRaw("Horizontal");
        int y = (int)Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(x) > Mathf.Abs(y)) y = 0;
        else if (Mathf.Abs(y) > Mathf.Abs(x)) x = 0;

        if (x == 0 && y == 0) return;

        TryMove(new Vector3Int(x, y, 0));
    }

    void TryMove(Vector3Int deltaCell)
    {
        Vector3Int currentCell = grid.WorldToCell(transform.position);
        Vector3Int nextCell = currentCell + deltaCell;
        Vector3 targetWorld = grid.GetCellCenterWorld(nextCell);

        bool blocked = IsBlockedAt(targetWorld);

        if (!blocked)
        {
            facing?.NotifyMoveDelta(deltaCell);    // <<< add this line
            StartCoroutine(MoveTo(targetWorld));
        }
    }

    IEnumerator MoveTo(Vector3 targetPos)
    {
        isMoving = true;
        Vector3 start = transform.position;
        float t = 0f;

        while (t < moveTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / moveTime);
            rb.MovePosition(Vector3.Lerp(start, targetPos, a));
            yield return null;
        }

        rb.MovePosition(targetPos);
        isMoving = false;
    }

    void SnapToGridCenter()
    {
        if (!grid) return;
        Vector3Int cell = grid.WorldToCell(transform.position);
        Vector3 center = grid.GetCellCenterWorld(cell);
        rb.position = center;
    }

    // ��� Collision checking that matches your collider shape ���
    bool IsBlockedAt(Vector3 worldCenter)
    {
        // default gizmo settings
        gizmoCenter = worldCenter;
        gizmoCapsuleDir = CapsuleDirection2D.Vertical;

        if (col is BoxCollider2D box)
        {
            Vector2 size = box.bounds.size;
            size.x = Mathf.Max(0.01f, size.x - skin * 2f);
            size.y = Mathf.Max(0.01f, size.y - skin * 2f);
            gizmoSize = size;
            gizmoShape = GizmoShape.Box;

            return Physics2D.OverlapBox(worldCenter, size, 0f, obstacleMask) != null;
        }
        else if (col is CapsuleCollider2D capsule)
        {
            Vector2 size = capsule.bounds.size;
            size.x = Mathf.Max(0.01f, size.x - skin * 2f);
            size.y = Mathf.Max(0.01f, size.y - skin * 2f);
            gizmoSize = size;
            gizmoCapsuleDir = capsule.direction;
            gizmoShape = GizmoShape.Capsule;

            return Physics2D.OverlapCapsule(worldCenter, size, capsule.direction, 0f, obstacleMask) != null;
        }
        else if (col is CircleCollider2D circle)
        {
            float radius = Mathf.Max(0.005f, circle.bounds.extents.x - skin);
            gizmoSize = new Vector2(radius * 2f, radius * 2f);
            gizmoShape = GizmoShape.Circle;

            return Physics2D.OverlapCircle(worldCenter, radius, obstacleMask) != null;
        }
        else
        {
            Vector2 size = col.bounds.size;
            size.x = Mathf.Max(0.01f, size.x - skin * 2f);
            size.y = Mathf.Max(0.01f, size.y - skin * 2f);
            gizmoSize = size;
            gizmoShape = GizmoShape.Box;

            return Physics2D.OverlapBox(worldCenter, size, 0f, obstacleMask) != null;
        }
    }

    void FixedUpdate()
    {
        if (!isMoving)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    } 

    void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;
        if (gizmoShape == GizmoShape.Box)
            Gizmos.DrawWireCube(gizmoCenter, new Vector3(gizmoSize.x, gizmoSize.y, 0.05f));
        else if (gizmoShape == GizmoShape.Circle)
            Gizmos.DrawWireSphere(gizmoCenter, gizmoSize.x * 0.5f);
        else
            Gizmos.DrawWireCube(gizmoCenter, new Vector3(gizmoSize.x, gizmoSize.y, 0.05f));
    }
}
