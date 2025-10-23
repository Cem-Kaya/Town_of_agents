using UnityEngine;

/// Toggles the visibility of the ShortcutRoot UI (or any assigned GameObject)
/// when Tab or a chosen key is pressed.
public class ShortcutToggleUI : MonoBehaviour
{
    [Header("Assign the root UI object (e.g. ShortcutRoot)")]
    public GameObject shortcutRoot;

    [Header("Toggle key settings")]
    [Tooltip("Main key used to show/hide the UI (default: Tab).")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Tooltip("Optional secondary key (set to None to disable).")]
    public KeyCode secondaryKey = KeyCode.None;

    [Header("Optional settings")]
    public bool startHidden = false;

    void Start()
    {
        if (!shortcutRoot)
        {
            Debug.LogWarning("[ShortcutToggleUI] ShortcutRoot not assigned.");
            enabled = false;
            return;
        }

        if (startHidden)
            shortcutRoot.SetActive(false);
    }

    void Update()
    {
        if (!shortcutRoot) return;

        bool pressed =
            Input.GetKeyDown(toggleKey) ||
            (secondaryKey != KeyCode.None && Input.GetKeyDown(secondaryKey));

        if (pressed)
            shortcutRoot.SetActive(!shortcutRoot.activeSelf);
    }
}
