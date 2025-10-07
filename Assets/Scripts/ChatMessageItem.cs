using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChatMessageItem : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text text;          // assign the TMP under the bubble
    public LayoutGroup optionalLayoutGroup; // (optional) helps rebuild after set
    public LayoutElement optionalLayoutElement;

    public void Set(string message)
    {
        if (text) text.text = message ?? "";
        // force a layout pass so height updates before we auto-scroll
        Canvas.ForceUpdateCanvases();
        if (optionalLayoutGroup) LayoutRebuilder.ForceRebuildLayoutImmediate(optionalLayoutGroup.transform as RectTransform);
        if (optionalLayoutElement) LayoutRebuilder.ForceRebuildLayoutImmediate(optionalLayoutElement.transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
}
