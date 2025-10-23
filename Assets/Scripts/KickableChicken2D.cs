// KickableChicken2D.cs
// Keeps kick impulse; adds optional boosted wall-bounce so rebounds feel lively.

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class KickableChicken2D : MonoBehaviour
{
    [HideInInspector] public ChickenSoccerManager2D manager;

    [Header("Kick Physics")]
    [Min(0f)] public float baseKickImpulse = 12f;
    [Min(0f)] public float maxKickImpulse = 22f;
    public float arcBiasY = 0.0f;

    [Header("Center Drift")]
    [Min(0f)] public float centerDriftSpeed = 0.6f;
    [Min(0f)] public float centerDriftAccel = 1.5f;
    [Min(0f)] public float driftKickCooldown = 0.35f;

    [Header("Bounce Boost (Walls)")]
    [Tooltip("If true, add a small extra impulse on static walls to keep rebounds lively.")]
    public bool exaggerateBounceOnWalls = true;

    [Tooltip("If the post-collision reflected speed would be below this, we boost it up to this threshold.")]
    [Min(0f)] public float minWallBounceSpeed = 3.5f;

    [Tooltip("Multiplier applied to the reflected velocity when boosting.")]
    [Min(0f)] public float wallBounceBoost = 1.05f;

    Rigidbody2D rb;
    float lastKickTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (manager == null) return;

        // pause drift right after a kick
        if (Time.time - lastKickTime < driftKickCooldown) return;

        Vector2 pos = transform.position;
        Vector2 toCenter = manager.Center - pos;
        float dist = toCenter.magnitude;
        if (dist < 0.01f) return;

        Vector2 desired = toCenter / dist * centerDriftSpeed; // target velocity
        Vector2 current = rb.linearVelocity;
        Vector2 dv = desired - current;

        float maxStep = centerDriftAccel * Time.fixedDeltaTime;
        if (dv.magnitude > maxStep)
            dv = dv.normalized * maxStep;

        rb.linearVelocity = current + dv;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // 1) Kicks from dynamic bodies (e.g., player)
        if (col.rigidbody != null)
        {
            Vector2 avgNormal = AverageNormal(col);
            float relSpeed = col.relativeVelocity.magnitude;

            Vector2 dir = -avgNormal;
            if (arcBiasY != 0f)
                dir = (dir + new Vector2(0f, Mathf.Clamp01(arcBiasY))).normalized;

            float impulseMag = Mathf.Min(baseKickImpulse * relSpeed, maxKickImpulse);
            rb.AddForce(dir * impulseMag, ForceMode2D.Impulse);

            lastKickTime = Time.time;
            return;
        }

        // 2) Static walls: optional bounce exaggeration
        if (exaggerateBounceOnWalls)
        {
            Vector2 n = AverageNormal(col);
            // velocity just before resolve is not directly available here,
            // but we can reflect current velocity to estimate the outgoing direction
            Vector2 v = rb.linearVelocity;
            if (v.sqrMagnitude > 1e-6f)
            {
                Vector2 reflected = Vector2.Reflect(v, n);
                float targetMag = Mathf.Max(reflected.magnitude, minWallBounceSpeed);
                Vector2 targetVel = reflected.normalized * (targetMag * wallBounceBoost);

                Vector2 dv = targetVel - v;
                rb.AddForce(dv * rb.mass, ForceMode2D.Impulse);
            }
        }
    }

    static Vector2 AverageNormal(Collision2D col)
    {
        Vector2 avg = Vector2.zero;
        int count = col.contactCount;
        if (count == 0) return -col.relativeVelocity.normalized;

        for (int i = 0; i < count; i++)
            avg += col.GetContact(i).normal;

        if (avg.sqrMagnitude < 1e-6f) return -col.relativeVelocity.normalized;
        return avg.normalized;
    }
}
