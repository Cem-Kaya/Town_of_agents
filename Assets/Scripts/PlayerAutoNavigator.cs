using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// Auto-navigation for the Player: grid A* + step movement.
/// Requires a Player + Rigidbody2D + a 2D collider on the same GameObject.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAutoNavigator : MonoBehaviour
{
    [Header("Scene")]
    public Grid grid;
    public Tilemap wallsTilemapOptional; // optional, to auto-derive area once
    public LayerMask obstacleMask;

    [Header("Navigation Area (cell coords)")]
    public BoundsInt navArea = new BoundsInt(-50, -50, 0, 100, 100, 1);

    [Header("Movement")]
    [Min(0.01f)] public float moveTimePerStep = 0.12f;  // match Player.moveTime
    [Tooltip("Extra safety shrink of collider when probing (meters).")]
    [Range(0f, 0.2f)] public float skin = 0.04f;

    Rigidbody2D rb;
    Collider2D col;
    Player player;               // your existing Player script
    bool navigating;
    Coroutine runner;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        player = GetComponent<Player>();
        if (!grid)
        {
            grid = FindAnyObjectByType<Grid>();
            if (!grid) Debug.LogError("[PlayerAutoNavigator] No Grid found/assigned.");
        }
    }

    void Start()
    {
        if (wallsTilemapOptional)
        {
            // expand by 2 cells so borders are covered
            var b = wallsTilemapOptional.cellBounds;
            navArea = new BoundsInt(b.xMin - 2, b.yMin - 2, 0, b.size.x + 4, b.size.y + 4, 1);
        }
    }

    public void CancelNavigation()
    {
        if (runner != null) StopCoroutine(runner);
        navigating = false;
        runner = null;
        if (player) player.SetInputEnabled(true);
    }

    public void GoToWorldPosition(Vector3 worldPos)
    {
        Vector3Int goal = grid.WorldToCell(worldPos);
        GoToCell(goal);
    }

    public void GoToTransform(Transform target)
    {
        if (!target) return;
        GoToWorldPosition(target.position);
    }

    public void GoToCell(Vector3Int goal)
    {
        if (!InBounds(goal) || !IsWalkableCell(goal))
        {
            // invalid target
            return;
        }

        Vector3Int start = grid.WorldToCell(transform.position);
        var path = FindPath(start, goal);

        if (path.Count == 0) return;

        if (runner != null) StopCoroutine(runner);
        runner = StartCoroutine(FollowPath(path));
    }

    IEnumerator FollowPath(Queue<Vector3Int> path)
    {
        navigating = true;
        if (player) player.SetInputEnabled(false);

        while (path.Count > 0)
        {
            Vector3Int nextCell = path.Dequeue();

            // safety recheck
            if (!InBounds(nextCell) || !IsWalkableCell(nextCell))
            {
                // path invalidated; stop
                break;
            }

            Vector3 targetWorld = grid.GetCellCenterWorld(nextCell);
            yield return MoveStep(targetWorld, moveTimePerStep);
        }

        navigating = false;
        runner = null;
        if (player) player.SetInputEnabled(true);
    }

    IEnumerator MoveStep(Vector3 target, float stepTime)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < stepTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / stepTime);
            rb.MovePosition(Vector3.Lerp(start, target, a));
            yield return null;
        }
        rb.MovePosition(target);
    }

    // ---------- Pathfinding ----------

    bool InBounds(Vector3Int c)
    {
        return c.x >= navArea.xMin && c.x < navArea.xMax &&
               c.y >= navArea.yMin && c.y < navArea.yMax;
    }

    bool IsWalkableCell(Vector3Int c)
    {
        // probe using this player's collider shape with a "skin" shrink
        Vector3 center = grid.GetCellCenterWorld(c);

        if (col is BoxCollider2D box)
        {
            Vector2 size = box.bounds.size;
            size.x = Mathf.Max(0.01f, size.x - skin * 2f);
            size.y = Mathf.Max(0.01f, size.y - skin * 2f);
            return Physics2D.OverlapBox(center, size, 0f, obstacleMask) == null;
        }
        else if (col is CapsuleCollider2D capsule)
        {
            Vector2 size = capsule.bounds.size;
            size.x = Mathf.Max(0.01f, size.x - skin * 2f);
            size.y = Mathf.Max(0.01f, size.y - skin * 2f);
            return Physics2D.OverlapCapsule(center, size, capsule.direction, 0f, obstacleMask) == null;
        }
        else if (col is CircleCollider2D circle)
        {
            float radius = Mathf.Max(0.005f, circle.bounds.extents.x - skin);
            return Physics2D.OverlapCircle(center, radius, obstacleMask) == null;
        }
        else
        {
            // fallback as box
            Vector2 size = col.bounds.size;
            size.x = Mathf.Max(0.01f, size.x - skin * 2f);
            size.y = Mathf.Max(0.01f, size.y - skin * 2f);
            return Physics2D.OverlapBox(center, size, 0f, obstacleMask) == null;
        }
    }

    Queue<Vector3Int> FindPath(Vector3Int start, Vector3Int goal)
    {
        Vector3Int[] dirs = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0,-1, 0),
        };

        var pq = new PriorityQueue<Vector3Int>();
        var came = new Dictionary<Vector3Int, Vector3Int>();
        var g = new Dictionary<Vector3Int, int>();

        pq.Enqueue(start, 0);
        came[start] = start;
        g[start] = 0;

        while (pq.Count > 0)
        {
            Vector3Int cur = pq.Dequeue();
            if (cur == goal) break;

            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3Int nxt = cur + dirs[i];
                if (!InBounds(nxt) || !IsWalkableCell(nxt)) continue;

                int ng = g[cur] + 1;
                if (!g.ContainsKey(nxt) || ng < g[nxt])
                {
                    g[nxt] = ng;
                    int h = Mathf.Abs(nxt.x - goal.x) + Mathf.Abs(nxt.y - goal.y);
                    int f = ng + h;
                    pq.Ensure(nxt, f);
                    came[nxt] = cur;
                }
            }
        }

        if (!came.ContainsKey(goal)) return new Queue<Vector3Int>();

        var stack = new Stack<Vector3Int>();
        var node = goal;
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
