using UnityEngine;

/// Builds a boolean grid of walkable cells for grid A* in 2D.
/// Walkability test = collider-shape overlap at cell center (no casts).
public class NavGrid
{
    // Stored data
    public readonly Grid Grid;            // scene Grid reference
    public readonly BoundsInt Area;       // baked cell-space bounds
    public readonly bool[,] Walkable;     // [width, height], origin at Area.min
    public readonly Vector3Int Origin;    // = Area.min (cell coords)

    public int Width => Area.size.x;
    public int Height => Area.size.y;

    private enum Shape { Box, Circle, Capsule }

    public NavGrid(
        Grid grid,
        BoundsInt area,
        Collider2D agentCollider,
        LayerMask obstacleMask,
        float skin = 0.04f,
        int extraErosion = 0)
    {
        Grid = grid;
        Area = area;
        Origin = area.min;
        Walkable = new bool[Width, Height];

        // Determine probe shape in world space (shrink by skin)
        Vector2 boxSize = Vector2.zero;
        float circleRadius = 0f;
        CapsuleDirection2D capDir = CapsuleDirection2D.Vertical;
        Shape shape;

        if (agentCollider is CapsuleCollider2D cap)
        {
            Vector2 s = cap.bounds.size; // world
            boxSize = new Vector2(Mathf.Max(0.01f, s.x - 2f * skin),
                                  Mathf.Max(0.01f, s.y - 2f * skin));
            capDir = cap.direction;
            shape = Shape.Capsule;
        }
        else if (agentCollider is CircleCollider2D circle)
        {
            circleRadius = Mathf.Max(0.005f, circle.bounds.extents.x - skin); // world
            shape = Shape.Circle;
        }
        else // Box or anything else → treat as box by bounds
        {
            Vector2 s = agentCollider.bounds.size; // world
            boxSize = new Vector2(Mathf.Max(0.01f, s.x - 2f * skin),
                                  Mathf.Max(0.01f, s.y - 2f * skin));
            shape = Shape.Box;
        }

        // 1) Raw walkability per cell
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Vector3Int cell = new Vector3Int(Origin.x + x, Origin.y + y, 0);
                Vector3 center = Grid.GetCellCenterWorld(cell);

                bool blocked =
                    (shape == Shape.Box && Physics2D.OverlapBox(center, boxSize, 0f, obstacleMask) != null) ||
                    (shape == Shape.Circle && Physics2D.OverlapCircle(center, circleRadius, obstacleMask) != null) ||
                    (shape == Shape.Capsule && Physics2D.OverlapCapsule(center, boxSize, capDir, 0f, obstacleMask) != null);

                Walkable[x, y] = !blocked;
            }
        }

        // 2) Erode near obstacles by agent radius (+ optional extra)
        int erosion = ComputeErosionCells(Grid, agentCollider) + Mathf.Max(0, extraErosion);
        if (erosion > 0) Erode(erosion);
    }

    // Approximate how many cells of padding we need based on agent radius vs cell size.
    static int ComputeErosionCells(Grid grid, Collider2D agent)
    {
        float cell = Mathf.Min(grid.cellSize.x, grid.cellSize.y);
        float radius =
            Mathf.Max(agent.bounds.extents.x, agent.bounds.extents.y); // world extents as radius
        return Mathf.Clamp(Mathf.CeilToInt(radius / Mathf.Max(0.0001f, cell)) - 1, 0, 8);
    }

    // Mark cells within 'cells' Manhattan distance from any blocked cell as unwalkable.
    void Erode(int cells)
    {
        var blocked = new bool[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (!Walkable[x, y]) blocked[x, y] = true;

        var outGrid = (bool[,])Walkable.Clone();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!Walkable[x, y]) { outGrid[x, y] = false; continue; }

                bool near = false;
                for (int dy = -cells; dy <= cells && !near; dy++)
                {
                    for (int dx = -cells; dx <= cells && !near; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) continue;
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) > cells) continue;
                        if (blocked[nx, ny]) near = true;
                    }
                }

                if (near) outGrid[x, y] = false;
            }
        }

        System.Array.Copy(outGrid, Walkable, outGrid.Length);
    }

    public bool InBounds(Vector3Int c)
        => c.x >= Area.xMin && c.x < Area.xMax && c.y >= Area.yMin && c.y < Area.yMax;

    public bool IsWalkable(Vector3Int c)
    {
        if (!InBounds(c)) return false;
        int x = c.x - Origin.x;
        int y = c.y - Origin.y;
        return Walkable[x, y];
    }
}
