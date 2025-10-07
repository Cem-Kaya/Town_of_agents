using UnityEngine;

[ExecuteAlways]
public class NPCPathGizmo : MonoBehaviour
{
    public NPCWalkerGrid npc;
    public Grid grid;
    public Color pathColor = Color.yellow;
    public Color nodeColor = Color.magenta;

    void OnValidate()
    {
        if (!npc) npc = GetComponent<NPCWalkerGrid>();
        if (!grid && npc) grid = npc.grid;
    }

    void OnDrawGizmos()
    {
        if (!npc || !grid || npc.debugPath == null || npc.debugPath.Count == 0) return;

        Vector3 prev = transform.position;
        Gizmos.color = nodeColor;

        foreach (var cell in npc.debugPath)
        {
            Vector3 p = grid.GetCellCenterWorld(cell);
            Gizmos.DrawSphere(p, Mathf.Min(grid.cellSize.x, grid.cellSize.y) * 0.08f);
        }

        Gizmos.color = pathColor;
        Vector3 last = transform.position;
        foreach (var cell in npc.debugPath)
        {
            Vector3 p = grid.GetCellCenterWorld(cell);
            Gizmos.DrawLine(last, p);
            last = p;
        }
    }
}
