using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryUI : MonoBehaviour
{
    [Header("Root & Visibility")]
    [Tooltip("Optional: if set, only this panel will be faded; else we fade the object this script is on.")]
    public RectTransform rootPanel;
    [Tooltip("Keyboard key to toggle the panel")]
    public KeyCode toggleKey = KeyCode.I;

    [Header("Keys UI")]
    public Transform keysContent;           // parent for key rows
    public GameObject keyRowPrefab;         // prefab with Image + TMP_Text

    [Header("Notes UI")]
    public Transform notesContent;          // parent for note rows
    public GameObject noteRowPrefab;        // prefab with 2x TMP_Text (title/body)

    [Header("Key visuals (optional)")]
    public List<KeyVisual> keyVisuals = new(); // id -> sprite/name

    // ---- internals ----
    Dictionary<string, KeyVisual> _map;
    CanvasGroup _cg;
    bool _visible = true;
    bool _bound;            // subscribed to Inventory.OnChanged
    bool _dirtyPending;     // changes happened while hidden

    void Awake()
    {
        _cg = (rootPanel ? rootPanel.GetComponent<CanvasGroup>() : null) ?? GetComponent<CanvasGroup>();
        if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();

        BuildMap();
        // Start hidden or shown based on current alpha
        _visible = _cg.alpha > 0.5f;
        ApplyVisibility(_visible);

        // bind after singleton exists
        StartCoroutine(BindWhenReady());
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetVisible(!_visible);
    }

    IEnumerator BindWhenReady()
    {
        while (Inventory.I == null) yield return null;

        if (!_bound)
        {
            Inventory.I.OnChanged += OnInventoryChanged;
            _bound = true;
        }

        // First build (even if hidden so it’s ready)
        Rebuild();
    }

    void OnEnable()
    {
        // If Inventory already exists but we weren’t bound (eg. component re-enabled), bind now.
        if (Inventory.I != null && !_bound)
        {
            Inventory.I.OnChanged += OnInventoryChanged;
            _bound = true;
        }
    }

    void OnDisable()
    {
        if (_bound && Inventory.I != null)
        {
            Inventory.I.OnChanged -= OnInventoryChanged;
            _bound = false;
        }
    }

    void OnDestroy()
    {
        if (_bound && Inventory.I != null)
        {
            Inventory.I.OnChanged -= OnInventoryChanged;
            _bound = false;
        }
    }

    void OnInventoryChanged()
    {
        // If hidden, mark dirty and rebuild when shown; else rebuild immediately.
        if (!_visible) { _dirtyPending = true; return; }
        Rebuild();
    }

    [ContextMenu("Force Rebuild UI")]
    public void Rebuild()
    {
        if (Inventory.I == null) return;

        // ---- Keys ----
        if (keysContent && keyRowPrefab)
        {
            foreach (Transform c in keysContent) Destroy(c.gameObject);
            foreach (var k in Inventory.I.keys)
            {
                var row = Instantiate(keyRowPrefab, keysContent);
                var icon = row.GetComponentInChildren<Image>(true);
                var txt = row.GetComponentInChildren<TMP_Text>(true);

                if (_map != null && _map.TryGetValue(k, out var vis))
                {
                    if (icon) icon.sprite = vis.icon;
                    if (txt) txt.text = string.IsNullOrEmpty(vis.displayName) ? k : vis.displayName;
                }
                else
                {
                    if (txt) txt.text = k;
                }
            }
        }

        // ---- Notes ----
        if (notesContent && noteRowPrefab)
        {
            foreach (Transform c in notesContent) Destroy(c.gameObject);
            foreach (var n in Inventory.I.notes)
            {
                var row = Instantiate(noteRowPrefab, notesContent);
                var texts = row.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length > 0) texts[0].text = string.IsNullOrEmpty(n.title) ? "(Note)" : n.title;
                if (texts.Length > 1) texts[1].text = n.body;
            }
        }

        _dirtyPending = false;
        // Debug.Log($"InventoryUI: Rebuilt -> Keys={Inventory.I.keys.Count}, Notes={Inventory.I.notes.Count}");
    }

    void BuildMap()
    {
        _map = new Dictionary<string, KeyVisual>();
        foreach (var kv in keyVisuals)
        {
            if (kv != null && !string.IsNullOrEmpty(kv.keyId))
                _map[kv.keyId] = kv;
        }
    }

    void ApplyVisibility(bool show)
    {
        // CanvasGroup-based hide: keeps component enabled & subscribed.
        _cg.alpha = show ? 1f : 0f;
        _cg.interactable = show;
        _cg.blocksRaycasts = show;
    }

    public void SetVisible(bool show)
    {
        _visible = show;
        ApplyVisibility(show);

        // If we missed updates while hidden, rebuild now that it’s visible
        if (_visible && _dirtyPending) Rebuild();
    }

    // Optional helpers for UI buttons
    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);
    public void Toggle() => SetVisible(!_visible);
}

[System.Serializable]
public class KeyVisual
{
    public string keyId;
    public string displayName;
    public Sprite icon;
}
