using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMPlayer : MonoBehaviour
{
    public AudioClip music;
    [Range(0f, 1f)] public float volume = 0.35f;

    void Awake()
    {
        var src = GetComponent<AudioSource>();
        src.clip = music;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;       // 2D (global)
        src.volume = volume;
        src.Play();
    }
}
