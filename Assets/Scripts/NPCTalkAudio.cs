using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NPCTalkAudio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] talkClips;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 0.8f;
    [Tooltip("How far the NPC should be audible in XY. Camera Z gap is added automatically.")]
    public float audibleRadiusXY = 6f;
    [Tooltip("Bypass 3D and play 2D for debugging.")]
    public bool force2D = false;

    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        Apply3DSettings();
    }

    void OnValidate()
    {
        if (src == null) src = GetComponent<AudioSource>();
        if (src) Apply3DSettings();
    }

    void Apply3DSettings()
    {
        if (!src) return;

        if (force2D)
        {
            src.spatialBlend = 0f; // fully 2D
            return;
        }

        src.spatialBlend = 1f; // fully 3D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;

        float zGap = 0f;
        var cam = Camera.main;
        if (cam) zGap = Mathf.Abs(cam.transform.position.z - transform.position.z);

        // ex: 6 (xy) + 10 (camera z) + 1 buffer = 17
        src.maxDistance = Mathf.Max(10f, audibleRadiusXY + zGap + 1f);
    }

    public void PlayTalk()
    {
        if (talkClips == null || talkClips.Length == 0)
        {
            Debug.LogWarning("NPCTalkAudio: no talk clips.", this);
            return;
        }

        var clip = talkClips[Random.Range(0, talkClips.Length)];
        if (clip == null) { Debug.LogWarning("NPCTalkAudio: null clip element.", this); return; }

        // diagnostics
        var cam = Camera.main;
        float dist = cam ? Vector3.Distance(cam.transform.position, transform.position) : -1f;
        Debug.Log($"NPCTalkAudio.PlayTalk -> clip={clip.name}, vol={volume}, spatialBlend={src.spatialBlend}, maxDist={src.maxDistance}, listenerDist={dist:F2}", this);

        src.PlayOneShot(clip, volume);
    }
}
