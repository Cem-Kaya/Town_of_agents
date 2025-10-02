using System.Collections;
using UnityEngine;

/// <summary>
/// Grid-precise, one-tile-per-input movement with solid collision against a Tilemap-based Obstacles layer.
/// Works in Unity 6. Attach to Player (with Rigidbody2D + BoxCollider2D).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class player : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Reference to your scene's Grid (parent of the Tilemaps).")]
    public Grid grid;

    [Header("Movement")]
    [Tooltip("Seconds to move one tile.")]
    [Min(0.01f)] public float moveTime = 0.12f;

    [Tooltip("Small inset to avoid grazing collider edges during overlap checks.")]
    [Range(0f, 0.2f)] public float skin = 0.04f;

    [Header("Collision")]
    [Tooltip("LayerMask for obstacles (assign your 'Obstacles' layer).")]
    public LayerMask obstacleMask;

    [Tooltip("If true, draws the destination check box in play mode.")]
    public bool debugGizmos = false;

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private bool isMoving;
    private Vector3 lastGizmoCenter;
    private Vector2 lastGizmoSize;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();

        if (grid == null)
        {
            grid = FindObjectOfType<Grid>();
            if (grid == null)
                Debug.LogError("GridMover2D: No Grid assigned and none found in scene.");
        }

        // Start exactly on a cell center to avoid drift.
        SnapToGridCenter();
    }

    void Update()
    {
        if (isMoving) return;

        // Read raw input (WASD/Arrows). Values are -1, 0, or 1.
        int x = (int)Input.GetAxisRaw("Horizontal");
        int y = (int)Input.GetAxisRaw("Vertical");

        // Prevent diagonal steps: prefer the axis with the larger absolute value; if equal, prefer horizontal.
        if (Mathf.Abs(x) > Mathf.Abs(y)) y = 0;
        else if (Mathf.Abs(y) > Mathf.Abs(x)) x = 0;

        if (x == 0 && y == 0) return;

        TryMove(new Vector3Int(x, y, 0));
    }

    private void TryMove(Vector3Int deltaCell)
    {
        // Current cell and desired cell
        Vector3Int currentCell = grid.WorldToCell(transform.position);
        Vector3Int nextCell = currentCell + deltaCell;

        // Convert desired cell to world center
        Vector3 targetWorld = grid.GetCellCenterWorld(nextCell);

        // Use the player's collider size (shrunk by skin) to test the destination
        Vector2 checkSize = GetCheckSize();

        bool blocked = Physics2D.OverlapBox(targetWorld, checkSize, 0f, obstacleMask) != null;

        if (!blocked)
        {
            StartCoroutine(MoveTo(targetWorld));
        }

        // Store for gizmos
        lastGizmoCenter = targetWorld;
        lastGizmoSize = checkSize;
    }

    private IEnumerator MoveTo(Vector3 targetPos)
    {
        isMoving = true;

        Vector3 start = transform.position;
        float t = 0f;

        // Smooth move over moveTime using Rigidbody2D for consistent physics
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / moveTime);
            Vector3 next = Vector3.Lerp(start, targetPos, alpha);
            rb.MovePosition(next);
            yield return null;
        }

        rb.MovePosition(targetPos);
        isMoving = false;
    }

    private void SnapToGridCenter()
    {
        if (grid == null) return;
        Vector3Int cell = grid.WorldToCell(transform.position);
        Vector3 center = grid.GetCellCenterWorld(cell);
        rb.position = center;
    }

    private Vector2 GetCheckSize()
    {
        // Start from collider size in world space, shrink by skin on each side
        Vector2 worldSize = box.bounds.size;
        worldSize.x = Mathf.Max(0.01f, worldSize.x - skin * 2f);
        worldSize.y = Mathf.Max(0.01f, worldSize.y - skin * 2f);
        return worldSize;
    }

    void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(lastGizmoCenter, new Vector3(lastGizmoSize.x, lastGizmoSize.y, 0.05f));
    }
}
