using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns N chickens that wander inside a circular area, stop-and-go, like real chickens.
/// Put this on an empty GameObject; set radius, sprites, and counts in the Inspector.
/// </summary>
public class ChickenWanderArea : MonoBehaviour
{
    [Header("Scale Variance")]
    [Tooltip("Randomly scales each chicken between 1.0 and this multiplier.")]
    [Min(1f)] public float maxScaleMultiplier = 1.5f;

    [Header("Rendering")]
    [Tooltip("Sorting order for chicken sprites (higher = drawn on top).")]
    public int sortingOrder = 5;

    [Header("Area")]
    [Min(0.1f)] public float radius = 6f;

    [Header("Leash / Return-to-center")]
    [Tooltip("If a chicken starts a move beyond radius * leashFactor, it will return inward on the next move.")]
    [Range(0.5f, 1.0f)] public float leashFactor = 0.92f;

    [Tooltip("Extra inward bias while walking when within this edge thickness (world units).")]
    [Min(0f)] public float edgeBiasThickness = 0.4f;

    [Tooltip("Strength of inward bias when near the edge (0..1). Higher = stronger pull toward center.")]
    [Range(0f, 1f)] public float edgeBiasStrength = 0.7f;

    [Header("Chickens")]
    [Tooltip("How many chickens to spawn.")]
    [Min(1)] public int chickenCount = 3;

    [Tooltip("Sprites to randomly assign to spawned chickens.")]
    public List<Sprite> chickenSprites = new List<Sprite>();

    [Header("Behavior")]
    [Tooltip("Units per second.")]
    [Min(0.05f)] public float moveSpeed = 1.5f;

    [Tooltip("How long a chicken walks each burst (seconds).")]
    public Vector2 walkTimeRange = new Vector2(0.6f, 1.6f);

    [Tooltip("How long a chicken idles between walks (seconds).")]
    public Vector2 idleTimeRange = new Vector2(0.8f, 2.0f);

    [Tooltip("Small pause between direction changes while walking (pecking vibe).")]
    public Vector2 microPauseRange = new Vector2(0.05f, 0.15f);

    [Tooltip("Min distance for each walk burst (keeps them from jittering).")]
    public float minStepDistance = 0.6f;

    [Tooltip("Max extra random distance to add per burst.")]
    public float extraStepDistance = 1.2f;

    [Header("Spawn")]
    [Tooltip("Try to keep first spawn positions apart.")]
    public bool spreadOnSpawn = true;

    [Tooltip("Random seed (-1 = use time)")]
    public int randomSeed = -1;

    void Start()
    {
        if (randomSeed != -1) Random.InitState(randomSeed);

        for (int i = 0; i < chickenCount; i++)
        {
            var go = new GameObject($"Chicken_{i}",
                typeof(SpriteRenderer), typeof(Rigidbody2D),
                typeof(CircleCollider2D), typeof(ChickenAgent2D));

            go.transform.SetParent(transform, worldPositionStays: true);

            // spawn position inside circle
            Vector2 pos = Random.insideUnitCircle * (radius * 0.9f);
            if (spreadOnSpawn && chickenCount > 1)
                pos += (pos.normalized * Random.Range(0.1f, 0.8f));
            go.transform.position = (Vector2)transform.position + pos;

            // components
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            sr.sprite = (chickenSprites != null && chickenSprites.Count > 0)
                ? chickenSprites[Random.Range(0, chickenSprites.Count)]
                : null;

            // random scale FIRST (so collider sizing below is correct)
            float scaleMul = Random.Range(1f, maxScaleMultiplier);
            go.transform.localScale = Vector3.one * scaleMul;

            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.GetComponent<CircleCollider2D>();
            col.isTrigger = false;          // set true if you want no collisions
            col.radius = 0.2f;              // adjust as needed for your art scale

            var agent = go.GetComponent<ChickenAgent2D>();
            agent.Init(this);
        }
    }

    public Vector2 RandomPointInArea()
    {
        return (Vector2)transform.position + Random.insideUnitCircle * radius;
    }

    public bool IsInside(Vector2 worldPos)
    {
        return Vector2.Distance(worldPos, transform.position) <= radius;
    }

    public float PickWalkTime() => Random.Range(walkTimeRange.x, walkTimeRange.y);
    public float PickIdleTime() => Random.Range(idleTimeRange.x, idleTimeRange.y);
    public float PickMicroPause() => Random.Range(microPauseRange.x, microPauseRange.y);
    public float PickStepDistance() => minStepDistance + Random.value * extraStepDistance;

    // Leash helpers
    public bool NeedsReturn(Vector2 pos)
    {
        float d = Vector2.Distance(pos, transform.position);
        return d > radius * leashFactor;
    }

    public Vector2 Center => transform.position;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        // visualize leash threshold
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radius * leashFactor);
    }
#endif
}
