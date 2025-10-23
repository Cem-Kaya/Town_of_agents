using UnityEngine;

public class ShortcutToNPC : MonoBehaviour
{
    [System.Serializable]
    public class Binding
    {
        [Tooltip("Key to press, e.g. F1, F2, Alpha1, Alpha2, etc.")]
        public KeyCode key = KeyCode.F1;

        [Tooltip("NPC transform to navigate to.")]
        public Transform npc;
    }

    public PlayerAutoNavigator playerNavigator;
    public Binding[] bindings;

    void Update()
    {
        if (!playerNavigator || bindings == null) return;

        for (int i = 0; i < bindings.Length; i++)
        {
            var b = bindings[i];
            if (b.npc == null) continue;

            if (Input.GetKeyDown(b.key))
            {
                playerNavigator.GoToTransform(b.npc);
            }
        }
    }
}
