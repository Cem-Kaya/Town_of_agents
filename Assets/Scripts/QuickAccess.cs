using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickAccess : MonoBehaviour
{
    [System.Serializable]
    public class ActionDef
    {
        public string key = "I";
        public Sprite icon;
        public Color iconWrapperColor = new Color(0.165f, 0.173f, 0.188f, 1f); // #2A2C30FF
        public Color keycapColor = new Color32(0x3A, 0x3D, 0x44, 0xFF); // #3A3D44FF
    }

    [Header("Assign in Inspector")]
    public RectTransform quickAccessBar;   // quickAccessRoot/QuickAccessBar
    public GameObject quickActionItemPrefab; // QuickActionItem prefab

    [Header("Demo (set 4 entries)")]
    public ActionDef[] actions = new ActionDef[4];

    private void Start()
    {
        if (!quickAccessBar || !quickActionItemPrefab || actions == null)
        {
            Debug.LogWarning("[QuickAccessBarDemo] Missing references.");
            return;
        }

        // clear any design-time children
        for (int i = quickAccessBar.childCount - 1; i >= 0; i--)
            Destroy(quickAccessBar.GetChild(i).gameObject);

        // spawn
        foreach (var a in actions)
        {
            var go = Instantiate(quickActionItemPrefab, quickAccessBar);

            // Icon wrapper color
            var iconWrapper = go.transform.Find("IconWrapper")?.GetComponent<Image>();
            if (iconWrapper) iconWrapper.color = a.iconWrapperColor;

            // Icon sprite
            var icon = go.transform.Find("IconWrapper/Icon")?.GetComponent<Image>();
            if (icon)
            {
                icon.sprite = a.icon;
                icon.preserveAspect = true;
            }

            // Keycap visuals
            var keycap = go.transform.Find("KeycapBtn")?.GetComponent<Image>();
            if (keycap) keycap.color = a.keycapColor;

            // Key label
            var keyLabel = go.transform.Find("KeycapBtn/Text (TMP)")?.GetComponent<TMP_Text>();
            if (keyLabel) keyLabel.text = a.key;
        }
    }
}
