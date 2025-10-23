// ChickenSoccerManager2D.cs
// Attach to an empty GameObject at the field center.

using System.Collections.Generic;
using UnityEngine;

public class ChickenSoccerManager2D : MonoBehaviour
{
    [Header("Field")]
    [Min(0.1f)] public float respawnRadius = 1.5f;
    public Collider2D fieldBoundsOptional;

    [Header("Chickens")]
    [Min(1)] public int chickenCount = 3;
    public List<Sprite> chickenSprites = new List<Sprite>();
    [Min(0f)] public float spawnKickJitter = 0.0f;
    public string chickenLayerName = "Default";
    public string chickenTag = "Chicken";

    [Header("Kick Tuning")]
    [Min(0f)] public float baseKickImpulse = 12f;

    [Header("Center Drift")]
    [Min(0f)] public float centerDriftSpeed = 0.6f;
    [Min(0f)] public float centerDriftAccel = 1.5f;
    [Min(0f)] public float driftKickCooldown = 0.35f;

    [Header("Friction & Bounce (2D Physics Material)")]
    [Tooltip("Friction of the chicken collider (0 = ice, 1 = very grippy).")]
    [Range(0f, 1f)] public float chickenFriction = 0.8f;

    [Tooltip("Bounciness of the chicken collider (0 = no bounce, 1 = super bouncy).")]
    [Range(0f, 1f)] public float chickenBounciness = 0.6f;

    [Tooltip("How friction combines with other materials (2D).")]
    public PhysicsMaterialCombine2D frictionCombine = PhysicsMaterialCombine2D.Average;

    [Tooltip("How bounciness combines with other materials (2D).")]
    public PhysicsMaterialCombine2D bounceCombine = PhysicsMaterialCombine2D.Maximum;

    [Header("Air/Rotational Damping")]
    [Min(0f)] public float linearDrag = 1.2f;
    [Min(0f)] public float angularDrag = 0.8f;

    [Header("FX Optional")]
    public GameObject goalVfxPrefab;
    public AudioClip goalSfx;
    [Range(0f, 1f)] public float goalSfxVolume = 0.9f;

    [Header("Random Seed")]
    public int randomSeed = -1;

    // Runtime
    private readonly List<KickableChicken2D> chickens = new();
    private int scoreLeft = 0;
    private int scoreRight = 0;

    public int ScoreLeft => scoreLeft;
    public int ScoreRight => scoreRight;
    public Vector2 Center => transform.position;

    // Runtime-generated 2D material
    PhysicsMaterial2D chickenMatRuntime;

    void Awake()
    {
        if (randomSeed != -1) Random.InitState(randomSeed);
    }

    void Start()
    {
        BuildRuntimeMaterial();
        SpawnAll();
    }

    void BuildRuntimeMaterial()
    {
        chickenMatRuntime = new PhysicsMaterial2D("ChickenMat_Runtime")
        {
            friction = chickenFriction,
            bounciness = chickenBounciness
        };
        chickenMatRuntime.frictionCombine = frictionCombine;
        chickenMatRuntime.bounceCombine = bounceCombine;
    }

    void OnValidate()
    {
        if (Application.isPlaying && chickenMatRuntime != null)
        {
            chickenMatRuntime.friction = chickenFriction;
            chickenMatRuntime.bounciness = chickenBounciness;
            chickenMatRuntime.frictionCombine = frictionCombine;
            chickenMatRuntime.bounceCombine = bounceCombine;
        }
    }

    void SpawnAll()
    {
        for (int i = 0; i < chickenCount; i++)
        {
            var c = SpawnSingle(i);
            chickens.Add(c);
        }
    }

    KickableChicken2D SpawnSingle(int index)
    {
        var go = new GameObject($"Chicken_{index}",
            typeof(SpriteRenderer), typeof(Rigidbody2D),
            typeof(CircleCollider2D), typeof(KickableChicken2D));

        go.transform.SetParent(transform, true);
        go.tag = chickenTag;

        if (!string.IsNullOrEmpty(chickenLayerName))
        {
            int layer = LayerMask.NameToLayer(chickenLayerName);
            if (layer >= 0) go.layer = layer;
        }

        var sr = go.GetComponent<SpriteRenderer>();
        if (chickenSprites != null && chickenSprites.Count > 0)
            sr.sprite = chickenSprites[Random.Range(0, chickenSprites.Count)];
        sr.sortingOrder = 5;

        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        var col = go.GetComponent<CircleCollider2D>();
        col.isTrigger = false;
        // usedByComposite is obsolete; remove it. If you need composite, set it on the CompositeCollider2D.
        col.radius = 0.22f;
        col.sharedMaterial = chickenMatRuntime;

        var kc = go.GetComponent<KickableChicken2D>();
        kc.manager = this;
        kc.baseKickImpulse = baseKickImpulse;

        // drift params
        kc.centerDriftSpeed = centerDriftSpeed;
        kc.centerDriftAccel = centerDriftAccel;
        kc.driftKickCooldown = driftKickCooldown;

        Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * respawnRadius * 0.5f;
        go.transform.position = pos;

        if (spawnKickJitter > 0f)
        {
            Vector2 v = Random.insideUnitCircle.normalized * spawnKickJitter;
            rb.linearVelocity = v;
        }

        return kc;
    }

    public void RespawnChicken(KickableChicken2D chicken, Vector2 fxPosition, bool scoredLeftGoal)
    {
        if (goalVfxPrefab != null)
        {
            var v = Instantiate(goalVfxPrefab, fxPosition, Quaternion.identity);
            Destroy(v, 3f);
        }
        if (goalSfx != null)
            AudioSource.PlayClipAtPoint(goalSfx, fxPosition, goalSfxVolume);

        if (scoredLeftGoal) scoreLeft++;
        else scoreRight++;

        StartCoroutine(RespawnRoutine(chicken));
    }

    System.Collections.IEnumerator RespawnRoutine(KickableChicken2D chicken)
    {
        chicken.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.6f);

        var rb = chicken.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector2 pos = Center + Random.insideUnitCircle * (respawnRadius * 0.35f);
        chicken.transform.position = pos;

        chicken.gameObject.SetActive(true);
    }

    public void OnChickenBounced(KickableChicken2D chicken) { /* optional */ }
}
