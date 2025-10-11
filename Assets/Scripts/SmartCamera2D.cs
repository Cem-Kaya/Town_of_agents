using UnityEngine;

/// Smooth "dead-zone" camera for grid games.
/// - Camera stays still while the target moves inside a safe area (inner margins).
/// - When target crosses the safe area, camera glides toward a position that
///   brings the target back inside, with an extra "pan" (overshoot) in that direction.
/// - Optional clamping to world bounds.
/// Attach this to your Main Camera (orthographic). Assign the Player as Target.
[RequireComponent(typeof(Camera))]
public class SmartCamera2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;              // Player

    [Header("Dead Zone (world units from screen edges)")]
    [Tooltip("How far from each screen edge before the camera starts moving (in world units).")]
    public float marginX = 2f;            // e.g., 2 tiles if tile size = 1
    public float marginY = 2f;

    [Header("Pan / Overshoot")]
    [Tooltip("Extra world units to pan in the direction the target is pushing the edge.")]
    public float panExtra = 2f;

    [Header("Smoothing")]
    [Tooltip("Bigger = snappier, smaller = floatier.")]
    public float smoothTime = 0.20f;      // seconds to ease
    Vector3 _vel;                          // for SmoothDamp

    [Header("Clamp (optional)")]
    public bool clampToBounds = false;
    public Bounds worldBounds;            // set via inspector or at runtime

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic)
        {
            Debug.LogWarning("SmartCamera2D expects an Orthographic camera.");
        }
        if (!target) target = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Current view extents
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // Build current view rect (centered at camera)
        Vector3 camPos = transform.position;
        Rect view = new Rect(camPos.x - halfW, camPos.y - halfH, halfW * 2f, halfH * 2f);

        // Safe rect is the view rect shrunk by margins
        Rect safe = Shrink(view, marginX, marginY);

        // Compute how far the target is outside the safe rect on each axis.
        Vector2 push = Vector2.zero;

        Vector3 tpos = target.position;

        // X axis
        if (tpos.x > safe.xMax) push.x = (tpos.x - safe.xMax) + panExtra;
        else if (tpos.x < safe.xMin) push.x = (tpos.x - safe.xMin) - panExtra;

        // Y axis
        if (tpos.y > safe.yMax) push.y = (tpos.y - safe.yMax) + panExtra;
        else if (tpos.y < safe.yMin) push.y = (tpos.y - safe.yMin) - panExtra;

        // If inside safe rect, don't move.
        if (push == Vector2.zero) return;

        // Desired camera position before clamping
        Vector3 desired = camPos + new Vector3(push.x, push.y, 0f);

        // Clamp to bounds if requested
        if (clampToBounds)
        {
            // Compute the max camera center such that the view still stays inside bounds
            float minX = worldBounds.min.x + halfW;
            float maxX = worldBounds.max.x - halfW;
            float minY = worldBounds.min.y + halfH;
            float maxY = worldBounds.max.y - halfH;

            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
        }

        // Smooth glide
        Vector3 newPos = Vector3.SmoothDamp(camPos, desired, ref _vel, smoothTime);
        newPos.z = camPos.z; // keep camera z
        transform.position = newPos;
    }

    Rect Shrink(Rect r, float dx, float dy)
    {
        return new Rect(r.x + dx, r.y + dy, r.width - dx * 2f, r.height - dy * 2f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (!cam) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = transform.position;

        // View rect
        DrawRectGizmo(new Rect(c.x - halfW, c.y - halfH, halfW * 2f, halfH * 2f), new Color(1, 1, 1, 0.2f));
        // Safe rect
        Rect safe = Shrink(new Rect(c.x - halfW, c.y - halfH, halfW * 2f, halfH * 2f), marginX, marginY);
        DrawRectGizmo(safe, new Color(0, 1, 0, 0.25f));

        if (clampToBounds && worldBounds.size != Vector3.zero)
        {
            Gizmos.color = new Color(1, 0.6f, 0, 0.2f);
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
    }

    void DrawRectGizmo(Rect r, Color col)
    {
        Gizmos.color = col;
        Vector3 p1 = new Vector3(r.xMin, r.yMin, 0);
        Vector3 p2 = new Vector3(r.xMax, r.yMin, 0);
        Vector3 p3 = new Vector3(r.xMax, r.yMax, 0);
        Vector3 p4 = new Vector3(r.xMin, r.yMax, 0);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
#endif
}
