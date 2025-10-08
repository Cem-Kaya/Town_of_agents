using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; // for Handles.Label (nice cell labels)
#endif

/// Visualizes a baked NavGrid with Gizmos (walkable/blocked cells + bounds).
[ExecuteAlways]
public class NavGridViewer : MonoBehaviour
{
    [Header("Nav Source")]
    public Grid grid;
    public Collider2D agentCollider;          // drag your NPC (any 2D collider)
    public LayerMask obstacleMask;

    [Header("Bake Area (cell coords)")]
    public BoundsInt area = new BoundsInt(-20, -20, 0, 40, 40, 1);
    [Min(0)] public int extraErosion = 0;

    [Header("Draw Options")]
    public bool drawWalkable = true;
    public bool drawBlocked = true;
    public bool drawCellCenters = false;
    public bool drawBoundsOutline = true;
    public bool showCellLabels = false;  // editor only labels (x,y)

    [Range(0f, 1f)] public float fillAlpha = 0.35f;

    NavGrid nav;

    void OnValidate() { if (!grid) grid = GetComponent<Grid>(); Rebuild(); }
    void OnEnable() { Rebuild(); }
    void OnDisable() { nav = null; }

    public void Rebuild()
    {
        if (!grid || !agentCollider) { nav = null; return; }
        nav = new NavGrid(grid, area, agentCollider, obstacleMask, 0.04f, extraErosion);
    }

    void OnDrawGizmos()
    {
        if (nav == null || grid == null) return;

        // bounds outline
        if (drawBoundsOutline)
        {
            Gizmos.color = Color.white;
            Vector3 min = grid.CellToWorld(nav.Area.min);
            Vector3 max = grid.CellToWorld(new Vector3Int(nav.Area.xMax, nav.Area.yMax, 0));
            Vector3 size = max - min;
            Gizmos.DrawWireCube(min + size * 0.5f, new Vector3(size.x, size.y, 0));
        }

        // cells
        for (int y = 0; y < nav.Height; y++)
            for (int x = 0; x < nav.Width; x++)
            {
                Vector3Int cell = new Vector3Int(nav.Origin.x + x, nav.Origin.y + y, 0);
                Vector3 c = grid.GetCellCenterWorld(cell);
                Vector3 s = (Vector3)grid.cellSize;

                bool canWalk = nav.Walkable[x, y];

                if (canWalk && drawWalkable)
                {
                    Gizmos.color = new Color(0f, 1f, 0f, fillAlpha);
                    Gizmos.DrawCube(c, new Vector3(s.x, s.y, 0.01f));
                }
                else if (!canWalk && drawBlocked)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, fillAlpha);
                    Gizmos.DrawCube(c, new Vector3(s.x, s.y, 0.01f));
                }

                if (drawCellCenters)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(c, Mathf.Min(s.x, s.y) * 0.05f);
                }

#if UNITY_EDITOR
                if (showCellLabels)
                    Handles.Label(c, $"({cell.x},{cell.y})");
#endif
            }
    }
}
