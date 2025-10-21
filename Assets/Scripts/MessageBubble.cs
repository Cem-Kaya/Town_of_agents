using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageBubble : MonoBehaviour
{
    [Header("Auto-wired (can be left empty)")]
    [SerializeField] private TMP_Text text;                 // child TMP
    [SerializeField] private RectTransform bubbleRect;      // this/background rect
    [SerializeField] private LayoutElement layoutElem;      // on bubble root

    [Header("Tuning")]
    [Tooltip("Extra pixels to subtract to keep a small gutter on the right.")]
    [SerializeField] private float rightGutter = 4f;

    // Cached groups for padding
    private HorizontalLayoutGroup hlg;
    private VerticalLayoutGroup vlg;

    void Awake()
    {
        if (!text) text = GetComponentInChildren<TMP_Text>(true);
        if (!bubbleRect) bubbleRect = transform as RectTransform;
        if (!layoutElem) layoutElem = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();

        // Optional: auto-disable conflicting fitters on this object
        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter) fitter.enabled = false;

        hlg = GetComponent<HorizontalLayoutGroup>();
        vlg = GetComponent<VerticalLayoutGroup>();

        // Make sure TMP is allowed to wrap
        if (text)
        {
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;    // or Truncate
            // Break long tokens later; set closer to 1 if you want to delay breaking even more.
            text.wordWrappingRatios = 0.0f;
        }

        // Layout should not stretch horizontally; we control preferredWidth
        layoutElem.flexibleWidth = 0f;
    }

    public void SetText(string value)
    {
        if (!text) return;
        text.text = value ?? string.Empty;
        text.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRect);
    }

    /// <summary>
    /// Limits bubble width to viewportMaxWidth minus bubble padding/margins,
    /// so wrapping happens as late (right) as expected.
    /// </summary>
    public void ClampWidth(float viewportMaxWidth)
    {
        if (viewportMaxWidth <= 0f) return;

        float pad = 0f;
        if (hlg) pad += hlg.padding.left + hlg.padding.right;
        if (vlg) pad += vlg.padding.left + vlg.padding.right;
        if (text)
        {
            // TMP margins: x = left, z = right
            pad += Mathf.Max(0, text.margin.x) + Mathf.Max(0, text.margin.z);
        }
        pad += Mathf.Max(0, rightGutter);

        float target = Mathf.Max(32f, viewportMaxWidth - pad);

        layoutElem.preferredWidth = target;   // key line
        layoutElem.minWidth = 0f;
        layoutElem.flexibleWidth = 0f;

        // Normalize any prefab-stretched width
        var sd = bubbleRect.sizeDelta;
        if (sd.x > target) sd.x = target;
        bubbleRect.sizeDelta = sd;

        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRect);
    }
}
