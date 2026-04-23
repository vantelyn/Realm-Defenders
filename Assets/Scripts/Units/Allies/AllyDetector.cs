using System.Collections.Generic;
using UnityEngine;

public class AllyDetector : MonoBehaviour
{
    [SerializeField] private LayerMask detectLayers;

    private readonly HashSet<PlayerUnit> detected = new HashSet<PlayerUnit>();

    public IReadOnlyCollection<PlayerUnit> Detected => detected;

    public void CleanNulls()
    {
        detected.RemoveWhere(u => u == null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerUnit unit = other.GetComponentInParent<PlayerUnit>();
        if (unit == null) return;
        if (((1 << other.gameObject.layer) & detectLayers) == 0) return;
        detected.Add(unit);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerUnit unit = other.GetComponentInParent<PlayerUnit>();
        if (unit == null) return;
        detected.Remove(unit);
    }
}