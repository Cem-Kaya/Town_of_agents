using System.Collections;
using UnityEngine;

/// <summary>
/// Simple stop-and-go wanderer that stays inside a circular area.
/// Created by ChickenWanderArea; you can also add manually and call Init(area).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class ChickenAgent2D : MonoBehaviour
{
    private ChickenWanderArea area;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private enum State { Idle, Walking }
    private State state = State.Idle;

    private Vector2 target;
    private float speed;
    private float nextStateTime;

    // tweak: how close is "reached target"
    private const float arriveEps = 0.06f;

    // internal flag: force next move to return inward
    private bool forceReturnNext = false;

    public void Init(ChickenWanderArea a)
    {
        area = a;
        speed = a.moveSpeed;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        StopAllCoroutines();
        StartCoroutine(Brain());
    }

    IEnumerator Brain()
    {
        // initial idle
        state = State.Idle;
        nextStateTime = Time.time + area.PickIdleTime();

        while (true)
        {
            switch (state)
            {
                case State.Idle:
                    // set/refresh leash decision while idle
                    forceReturnNext = area.NeedsReturn(transform.position);

                    if (Time.time >= nextStateTime)
                    {
                        if (forceReturnNext)
                            BeginReturnBurst();
                        else
                            BeginWalkBurst();
                    }
                    break;

                case State.Walking:
                    TickWalk();

                    bool reached = Vector2.Distance(transform.position, target) <= arriveEps;
                    bool timeUp = Time.time >= nextStateTime;

                    if (reached || timeUp)
                    {
                        state = State.Idle;
                        nextStateTime = Time.time + area.PickIdleTime();
                        rb.linearVelocity = Vector2.zero;
                        // after a return, allow normal wandering again
                        forceReturnNext = false;
                    }
                    break;
            }

            yield return null;
        }
    }

    void BeginReturnBurst()
    {
        // aim toward center with a normal step distance
        float step = area.PickStepDistance();
        Vector2 pos = transform.position;
        Vector2 toCenter = ((Vector2)area.Center - pos);
        Vector2 dir = toCenter.sqrMagnitude > 1e-6f ? toCenter.normalized : Random.insideUnitCircle.normalized;

        // pick a target a "step" toward center, but don't overshoot past center
        float distToCenter = toCenter.magnitude;
        float travel = Mathf.Min(step, distToCenter * 0.9f);
        target = pos + dir * travel;

        state = State.Walking;
        nextStateTime = Time.time + area.PickWalkTime();

        SetVelocityTowards(target);

        StopCoroutineSafely(nameof(MicroPauseRoutine));
        StartCoroutine(MicroPauseRoutine());
    }

    void BeginWalkBurst()
    {
        // choose a random direction and distance but keep end-point inside circle
        float step = area.PickStepDistance();
        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector2 candidate = (Vector2)transform.position + dir * step;

        // if candidate outside, aim toward center a bit
        if (!area.IsInside(candidate))
        {
            dir = ((Vector2)area.Center - (Vector2)transform.position).normalized;
            candidate = (Vector2)transform.position + dir * step * 0.9f;
        }

        target = candidate;
        state = State.Walking;

        // walk for a limited time
        float walkTime = area.PickWalkTime();
        nextStateTime = Time.time + walkTime;

        // set initial velocity
        SetVelocityTowards(target);

        // micro-pauses during walking
        StopCoroutineSafely(nameof(MicroPauseRoutine));
        StartCoroutine(MicroPauseRoutine());
    }

    void TickWalk()
    {
        SetVelocityTowards(target);

        // face based on velocity x
        var v = rb.linearVelocity;
        if (Mathf.Abs(v.x) > 0.01f)
            sr.flipX = v.x < 0f;
    }

    IEnumerator MicroPauseRoutine()
    {
        while (state == State.Walking)
        {
            // wait some time, then quick pause to emulate peck/hesitate
            yield return new WaitForSeconds(area.PickMicroPause());
            if (state != State.Walking) break;

            Vector2 oldVel = rb.linearVelocity;
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.05f);

            // Slight nudge in a new nearby direction
            Vector2 jitter = (Random.insideUnitCircle * 0.3f);
            Vector2 toTarget = ((Vector2)target - (Vector2)transform.position).normalized;
            Vector2 newDir = (toTarget + jitter).normalized;

            rb.linearVelocity = newDir * speed;
        }
    }

    void SetVelocityTowards(Vector2 tgt)
    {
        Vector2 pos = transform.position;
        Vector2 dir = (tgt - pos);
        float dist = dir.magnitude;
        if (dist < arriveEps)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        dir /= Mathf.Max(dist, 0.0001f);
        Vector2 vel = dir * speed;

        // Edge bias: if near the boundary, blend in a push toward center
        float dToCenter = Vector2.Distance(pos, area.Center);
        float dToEdge = area.radius - dToCenter; // negative if outside
        if (dToEdge <= area.edgeBiasThickness)
        {
            float t = Mathf.InverseLerp(area.edgeBiasThickness, 0f, Mathf.Clamp(dToEdge, 0f, area.edgeBiasThickness));
            float k = area.edgeBiasStrength * Mathf.Clamp01(t);
            if (k > 0f)
            {
                Vector2 bias = (area.Center - pos).normalized;
                vel = Vector2.Lerp(vel, bias * speed, k);
            }
        }

        // If outside (can happen from collisions), strongly bias inward
        if (!area.IsInside(pos))
        {
            Vector2 strongBias = (area.Center - pos).normalized * speed;
            vel = Vector2.Lerp(vel, strongBias, 0.85f);
        }

        rb.linearVelocity = vel;
    }

    void StopCoroutineSafely(string routineName)
    {
        // Stop by name if running; harmless if it isn't
        StopCoroutine(routineName);
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (area == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target, 0.1f);
    }
#endif
}
