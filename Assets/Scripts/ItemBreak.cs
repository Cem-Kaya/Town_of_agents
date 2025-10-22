using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemBreak : MonoBehaviour
{
    [Header("Effects")]
    [Tooltip("Optional particle effect to spawn when the item breaks.")]
    public ParticleSystem breakVfx;

    [Tooltip("Optional audio clip to play when breaking.")]
    public AudioClip breakSfx;

    [Tooltip("How long to wait before destroying or replacing the object.")]
    public float delayBeforeDestroy = 0.25f;

    [Tooltip("If set, this prefab will replace the object after breaking.")]
    public GameObject brokenReplacementPrefab;

    private AudioSource audioSource;

    void Awake()
    {
        // Try to reuse existing AudioSource or add one if missing
        audioSource = GetComponent<AudioSource>();
        if (!audioSource && breakSfx)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Call this from InteractableItem.onInteract to break the object.
    /// </summary>
    public void BreakItem()
    {
        StartCoroutine(DoBreak());
    }

    private IEnumerator DoBreak()
    {
        // VFX
        if (breakVfx)
        {
            Instantiate(breakVfx, transform.position, Quaternion.identity);
        }

        // SFX
        if (breakSfx)
        {
            if (audioSource)
                audioSource.PlayOneShot(breakSfx);
            else
                AudioSource.PlayClipAtPoint(breakSfx, transform.position);
        }

        // Optional short delay to let effects play
        if (delayBeforeDestroy > 0f)
            yield return new WaitForSeconds(delayBeforeDestroy);

        // Replace with broken prefab if assigned
        if (brokenReplacementPrefab)
        {
            Instantiate(brokenReplacementPrefab, transform.position, transform.rotation);
        }

        // Finally destroy this object
        Destroy(gameObject);
    }
}
