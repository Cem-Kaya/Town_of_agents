using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCWalkerGrid : MonoBehaviour
{
    [Header("Scene")]
    public Grid grid;
    public Tilemap wallsTilemapOptional; // optional, only to derive bake bounds
    public LayerMask obstacleMask;

    [Header("Movement")]
    [Min(0.01f)] public float moveTime = 0.18f;

    [Header("Bake Area (cell coords)")]
    public BoundsInt bakeArea = new BoundsInt(-50, -50, 0, 100, 100, 1);
    [Tooltip("Extra safety erosion (cells) in addition to agent radius erosion.")]
    [Min(0)] public int extraErosion = 0;

    [Header("Random Walk (idle only)")]
    public bool enableRandomWalk = true;
    [Min(0.1f)] public float wanderTick = 1.0f;
    [Range(0, 1)] public float stepProbability = 0.4f;

    [Header("Inspector Trigger")]
    public int inspectorCellX = 0, inspectorCellY = 0;
    public bool goToCellTrigger = false;    // auto-resets
    public bool warpToCellTrigger = false;  // auto-resets
    [Min(0)] public float inspectorCooldown = 0.2f;
    float lastInspectorFire = -999f;

    Rigidbody2D rb;
    Collider2D col;

    NavGrid nav;
    enum State { Idle, Wandering, Navigating }
    State state = State.Idle;


    // NEW: allow disabling AI
    bool aiEnabled = true;
    public void SetAIEnabled(bool on)
    {
        aiEnabled = on;
        if (!aiEnabled)
        {
            StopAllCoroutines();
            isMoving = false;
            path.Clear();
            // stay exactly on current cell center
            Vector3Int c = grid.WorldToCell(transform.position);
            GetComponent<Rigidbody2D>().position = grid.GetCellCenterWorld(c);
        }
        else
        {
            // resume idle wander if enabled
            if (enableRandomWalk) StartCoroutine(WanderLoop());
        }
    }



    bool isMoving;
    Queue<Vector3Int> path = new Queue<Vector3Int>();
    Vector3Int goal;

    // for gizmo/overlay viewers
    public List<Vector3Int> debugPath = new List<Vector3Int>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (!grid) grid = Object.FindFirstObjectByType<Grid>();

        // Center on a cell
        Vector3Int c = grid.WorldToCell(transform.position);
        rb.position = grid.GetCellCenterWorld(c);
    }

    void Start()
    {
        // If you have a Walls tilemap, base the bake area on it
        if (wallsTilemapOptional)
        {
            BoundsInt b = wallsTilemapOptional.cellBounds;
            bakeArea = new BoundsInt(b.xMin - 2, b.yMin - 2, 0, b.size.x + 4, b.size.y + 4, 1);
        }

        BakeNav();
        if (enableRandomWalk) StartCoroutine(WanderLoop());
    }

    void BakeNav()
    {
        nav = new NavGrid(grid, bakeArea, col, obstacleMask, 0.04f, extraErosion);
    }

    void Update()
    {
        if (!aiEnabled) return;

        // Inspector triggers
        if (goToCellTrigger || warpToCellTrigger)
        {
            if (Time.unscaledTime - lastInspectorFire >= inspectorCooldown)
            {
                lastInspectorFire = Time.unscaledTime;
                Vector3Int tgt = new Vector3Int(inspectorCellX, inspectorCellY, 0);
                if (warpToCellTrigger) WarpToCell(tgt);
                else GoToCell(tgt);
            }
            goToCellTrigger = false;
            warpToCellTrigger = false;
        }

        if (isMoving) return;

        if (state == State.Navigating)
        {
            if (path.Count == 0)
            {
                state = enableRandomWalk ? State.Wandering : State.Idle;
                return;
            }

            Vector3Int nxt = path.Dequeue();

            // Safety: ensure next node is still walkable
            if (!nav.IsWalkable(nxt)) { state = State.Idle; path.Clear(); return; }

            StartCoroutine(MoveTo(grid.GetCellCenterWorld(nxt)));
        }
    }

    IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        Vector3 start = transform.position;
        float t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            rb.MovePosition(Vector3.Lerp(start, target, t / moveTime));
            yield return null;
        }
        rb.MovePosition(target);
        isMoving = false;
    }

    IEnumerator WanderLoop()
    {
        if (!aiEnabled) yield break;


        Vector3Int[] dirs = new Vector3Int[]
        {
            new Vector3Int( 1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 1, 0),
            new Vector3Int( 0,-1, 0),
        };

        System.Random rng = new System.Random();

        while (true)
        {
            yield return new WaitForSeconds(wanderTick);
            if (!enableRandomWalk || state != State.Wandering || isMoving) continue;
            if (Random.value > stepProbability) continue;

            int startIdx = rng.Next(0, 4);
            Vector3Int cur = grid.WorldToCell(transform.position);

            for (int k = 0; k < 4; k++)
            {
                int i = (startIdx + k) % 4;
                Vector3Int nxt = cur + dirs[i];
                if (!nav.InBounds(nxt) || !nav.IsWalkable(nxt)) continue;

                StartCoroutine(MoveTo(grid.GetCellCenterWorld(nxt)));
                break;
            }
        }
    }

    // Public control
    public void GoToCell(Vector3Int targetCell)
    {
        if (!nav.InBounds(targetCell) || !nav.IsWalkable(targetCell))
        {
            state = State.Idle;
            path.Clear();
            debugPath.Clear();
            return;
        }

        goal = targetCell;
        Vector3Int start = grid.WorldToCell(transform.position);

        Queue<Vector3Int> newPath = FindPath(start, goal, nav);
        path = newPath;
        debugPath = new List<Vector3Int>(newPath); // copy for gizmo viewers
        state = (path.Count > 0) ? State.Navigating : State.Idle;
    }

    public void WarpToCell(Vector3Int cell)
    {
        if (!nav.InBounds(cell) || !nav.IsWalkable(cell)) return;
        rb.position = grid.GetCellCenterWorld(cell);
        path.Clear();
        debugPath.Clear();
        state = enableRandomWalk ? State.Wandering : State.Idle;
    }

    // Pure grid A* (4-way)
    Queue<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, NavGrid navGrid)
    {
        PriorityQueue<Vector3Int> pq = new PriorityQueue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> came = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, int> g = new Dictionary<Vector3Int, int>();

        Vector3Int[] dirs = new Vector3Int[]
        {
            new Vector3Int( 1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 1, 0),
            new Vector3Int( 0,-1, 0),
        };

        pq.Enqueue(start, 0);
        came[start] = start;
        g[start] = 0;

        while (pq.Count > 0)
        {
            Vector3Int cur = pq.Dequeue();
            if (cur == goal) break;

            foreach (Vector3Int d in dirs)
            {
                Vector3Int nxt = cur + d;
                if (!navGrid.InBounds(nxt) || !navGrid.IsWalkable(nxt)) continue;

                int ng = g[cur] + 1;
                if (!g.ContainsKey(nxt) || ng < g[nxt])
                {
                    g[nxt] = ng;
                    int f = ng + Mathf.Abs(nxt.x - goal.x) + Mathf.Abs(nxt.y - goal.y);
                    pq.Ensure(nxt, f);
                    came[nxt] = cur;
                }
            }
        }

        if (!came.ContainsKey(goal)) return new Queue<Vector3Int>();

        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        Vector3Int node = goal;
        while (node != start)
        {
            stack.Push(node);
            node = came[node];
        }
        return new Queue<Vector3Int>(stack);
    }

    // Tiny PQ
    class PriorityQueue<T>
    {
        private readonly List<(T item, int pri)> data = new List<(T item, int pri)>();
        public int Count => data.Count;

        public void Enqueue(T item, int pri) => Ensure(item, pri);

        public void Ensure(T item, int pri)
        {
            data.Add((item, pri));
            int i = data.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (data[i].pri >= data[p].pri) break;
                (data[i], data[p]) = (data[p], data[i]);
                i = p;
            }
        }

        public T Dequeue()
        {
            int li = data.Count - 1;
            T front = data[0].item;
            data[0] = data[li];
            data.RemoveAt(li);

            li = data.Count - 1;
            int p = 0;
            while (true)
            {
                int c = p * 2 + 1;
                if (c > li) break;
                int rc = c + 1;
                if (rc <= li && data[rc].pri < data[c].pri) c = rc;
                if (data[p].pri <= data[c].pri) break;
                (data[p], data[c]) = (data[c], data[p]);
                p = c;
            }
            return front;
        }
    }
}
