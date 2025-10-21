using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageBubble : MonoBehaviour
{
    [Header("Auto-wired (can be left empty)")]
    [SerializeField] private TMP_Text text;                 // any TMP child
    [SerializeField] private LayoutElement layoutElement;   // on the bubble Image
    [SerializeField] private RectTransform bubbleRect;      // bubble RectTransform

    private void Reset()
    {
        text = GetComponentInChildren<TMP_Text>(true);
        layoutElement = GetComponentInChildren<LayoutElement>(true);
        bubbleRect = GetComponentInChildren<RectTransform>(true);
    }

    public void SetText(string value)
    {
        if (text) text.text = value;
    }

    public void ClampWidth(float maxWidth)
    {
        if (!bubbleRect) bubbleRect = transform as RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRect);

        float preferred = (text ? text.preferredWidth : 300f) + 36f; // padding
        float target = Mathf.Min(preferred, maxWidth);

        if (!layoutElement)
        {
            layoutElement = gameObject.GetComponentInChildren<LayoutElement>();
            if (!layoutElement) layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = target;
        layoutElement.enabled = true;
    }
}
