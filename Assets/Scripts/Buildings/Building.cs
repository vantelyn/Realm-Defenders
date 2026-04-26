using System.Collections.Generic;
using UnityEngine;
using Game.Units;

namespace Game.Buildings
{

public class Building : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField] private int capacity = 1;
    [SerializeField] private Transform[] slots;

    [Header("Bonus")]
    [SerializeField] private float rangeMultiplier = 1.5f;

    [Header("Door")]
    [SerializeField] private Transform doorPoint;

    private readonly List<PlayerUnit> occupants = new List<PlayerUnit>();

    public int Capacity => capacity;
    public int FreeSlots => capacity - occupants.Count;
    public bool HasFreeSlot => FreeSlots > 0;
    public float RangeMultiplier => rangeMultiplier;
    public Vector3 DoorPosition => doorPoint != null ? doorPoint.position : transform.position;
    public IReadOnlyList<PlayerUnit> Occupants => occupants;

    public bool Contains(PlayerUnit unit) => occupants.Contains(unit);

    public bool TryEnter(PlayerUnit unit)
    {
        if (unit == null || !HasFreeSlot || occupants.Contains(unit)) return false;

        Transform slot = GetFreeSlot();
        if (slot == null) return false;

        occupants.Add(unit);
        unit.OnEnteredBuilding(this, slot);
        return true;
    }

    public void Exit(PlayerUnit unit)
    {
        if (unit == null || !occupants.Remove(unit)) return;
        unit.OnExitedBuilding(DoorPosition);
    }

    private Transform GetFreeSlot()
    {
        if (slots == null || slots.Length == 0) return transform;
        for (int i = 0; i < slots.Length; i++)
        {
            Transform s = slots[i];
            if (s == null) continue;
            bool taken = false;
            for (int j = 0; j < occupants.Count; j++)
            {
                if (occupants[j] != null && Vector3.Distance(occupants[j].transform.position, s.position) < 0.01f)
                {
                    taken = true; break;
                }
            }
            if (!taken) return s;
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if (doorPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(doorPoint.position, 0.25f);
        }
        if (slots != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform s in slots)
            {
                if (s != null) Gizmos.DrawWireSphere(s.position, 0.2f);
            }
        }
    }
}
}
