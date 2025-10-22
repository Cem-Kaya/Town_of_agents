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
                    // wait then start a new walk burst
                    if (Time.time >= nextStateTime)
                    {
                        BeginWalkBurst();
                    }
                    break;

                case State.Walking:
                    // walk toward target; occasionally micro-pause/change direction a little
                    TickWalk();

                    bool reached = Vector2.Distance(transform.position, target) <= arriveEps;
                    bool timeUp = Time.time >= nextStateTime;

                    if (reached || timeUp)
                    {
                        // stop, idle for a bit
                        state = State.Idle;
                        nextStateTime = Time.time + area.PickIdleTime();
                        rb.linearVelocity = Vector2.zero;
                        // tiny peck chance: quick micro-pause animation hook here if you add Animator
                    }
                    break;
            }

            yield return null;
        }
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
            dir = (((Vector2)area.transform.position) - (Vector2)transform.position).normalized;
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
        rb.linearVelocity = dir * speed;

        // keep inside circle: if we�re heading out, bias velocity toward center
        Vector2 center = area.transform.position;
        float dToEdge = area.radius - Vector2.Distance(pos, center);
        if (dToEdge < 0.2f)
        {
            Vector2 bias = (center - pos).normalized * 0.6f;
            rb.linearVelocity = (rb.linearVelocity.normalized * 0.7f + bias).normalized * speed;
        }
    }

    void StopCoroutineSafely(string routineName)
    {
        var c = GetComponent<MonoBehaviour>();
        // (We�re the MonoBehaviour � just shorthand.)
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
