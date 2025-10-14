using System;
using System.Collections.Generic;
using UnityEngine;

// SINGOLETON 
public class Inventory : MonoBehaviour
{
    public static Inventory I { get; private set; }

    [Header("Limits")]
    [Min(1)] public int maxKeys = 8;
    [Min(1)] public int maxNotes = 8;

    // keys are identified by an ID string like "red_key", "boss_key"
    public readonly List<string> keys = new();
    // notes are simple text entries; you can expand this later
    public readonly List<NoteData> notes = new();

    public event Action OnChanged;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    public bool HasKey(string keyId) => keys.Contains(keyId);

    public bool TryAddKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId) || keys.Count >= maxKeys) return false;
        if (keys.Contains(keyId)) return false; // no duplicates; remove if you want stacks
        keys.Add(keyId);
        OnChanged?.Invoke();
        return true;
    }

    public bool TryUseKey(string keyId)
    {
        // consume one key
        int idx = keys.IndexOf(keyId);
        if (idx < 0) return false;
        keys.RemoveAt(idx);
        OnChanged?.Invoke();
        return true;
    }

    public bool TryAddNote(string title, string body)
    {
        if (notes.Count >= maxNotes) return false;
        notes.Add(new NoteData { title = title, body = body, time = DateTime.Now });
        OnChanged?.Invoke();
        return true;
    }
}

[Serializable]
public class NoteData
{
    public string title;
    [TextArea(2, 6)] public string body;
    public DateTime time;
}
