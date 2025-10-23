// GoalTrigger2D.cs
// Unchanged. Included for completeness.

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GoalTrigger2D : MonoBehaviour
{
    public ChickenSoccerManager2D manager;
    public bool isLeftGoal = false;
    public string chickenTag = "Chicken";

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(chickenTag)) return;

        var chicken = other.GetComponent<KickableChicken2D>();
        if (chicken == null || manager == null) return;

        Vector2 hitPos = other.transform.position;
        manager.RespawnChicken(chicken, hitPos, isLeftGoal);
    }
}
