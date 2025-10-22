using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCShortcut : MonoBehaviour
{
    [System.Serializable]
    public class Row
    {
        public string name = "Farmer Hans";
        public string key = "F1";
        public Sprite avatar;
    }

    [Header("Assign in Inspector")]
    public RectTransform shortcutContent;     // ShortcutRoot/ShortcutPanel/ShortcutContent
    public GameObject npcListItemPrefab;      // The NPCListItem prefab (your structure)

    [Header("Demo rows")]
    public Row[] rows;

    private void Start()
    {
        if (!shortcutContent || !npcListItemPrefab || rows == null)
        {
            Debug.LogWarning("[NPCShortcut] Assign Content + Prefab + Rows.");
            return;
        }

        // Clear existing children (design-time copies)
        for (int i = shortcutContent.childCount - 1; i >= 0; i--)
            Destroy(shortcutContent.GetChild(i).gameObject);

        // Spawn rows
        foreach (var r in rows)
        {
            var go = Instantiate(npcListItemPrefab, shortcutContent);

            // Name
            var nameTMP = go.transform.Find("NPC_Name")?.GetComponent<TMP_Text>();
            if (nameTMP) nameTMP.text = r.name;

            // Shortcut key (button label)
            var keyTMP = go.transform.Find("ShortcutBtn/Text (TMP)")?.GetComponent<TMP_Text>();
            if (keyTMP) keyTMP.text = r.key;

            // Avatar sprite
            var avatarImg = go.transform.Find("Avatar/AvatarImage")?.GetComponent<Image>();
            if (avatarImg)
            {
                avatarImg.sprite = r.avatar;
                avatarImg.preserveAspect = true;
            }
        }
    }
}
