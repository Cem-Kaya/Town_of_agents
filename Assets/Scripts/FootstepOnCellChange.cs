using UnityEngine;

[RequireComponent(typeof(Transform))]
[DisallowMultipleComponent]
public class FootstepOnCellChange : MonoBehaviour
{
    [Header("Grid + Clips")]
    public Grid grid;                    // assign your scene Grid
    public AudioClip[] clips;            // 1..N step sounds

    [Header("Playback")]
    [Range(0f, 1f)] public float volume = 0.6f;
    [Range(0f, 1f)] public float pitchJitter = 0.05f;

    [Header("Spatial")]
    [Tooltip("0 = 2D (always audible). 1 = 3D with distance falloff.")]
    [Range(0f, 1f)] public float spatialBlend = 0f;  // Player = 0f, NPC = 1f
    [Tooltip("3D min distance (no attenuation).")]
    public float minDistance = 1f;
    [Tooltip("3D max distance (silent at this range).")]
    public float maxDistance = 6f;

    AudioSource src;
    Vector3Int lastCell;
    bool firstRun = true;

    void Awake()
    {
        if (!grid) grid = FindFirstObjectByType<Grid>();

        src = gameObject.GetComponent<AudioSource>();
        if (!src) src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = spatialBlend;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
    }

    void LateUpdate()
    {
        if (!grid || clips == null || clips.Length == 0) return;

        var cell = grid.WorldToCell(transform.position);

        if (firstRun)
        {
            lastCell = cell;
            firstRun = false;
            return;
        }

        if (cell != lastCell) // moved one tile
        {
            lastCell = cell;
            PlayStep();
        }
    }

    void PlayStep()
    {
        var clip = clips[Random.Range(0, clips.Length)];
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.volume = volume;
        src.PlayOneShot(clip, volume);
    }
}
