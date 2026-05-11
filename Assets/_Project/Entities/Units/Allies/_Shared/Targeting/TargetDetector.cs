using System.Collections.Generic;
using UnityEngine;

namespace Game.Targeting
{

public class TargetDetector : MonoBehaviour
{
    [SerializeField] private LayerMask detectLayers;

    private readonly HashSet<Transform> detected = new HashSet<Transform>();

    public IReadOnlyCollection<Transform> Detected => detected;

    public Transform FindClosest(Vector3 from)
    {
        Transform best = null;
        float bestSqr = float.MaxValue;
        foreach (Transform t in detected)
        {
            if (t == null) continue;
            float sqr = ((Vector2)(t.position - from)).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }
        return best;
    }

    public void Forget(Transform t)
    {
        detected.Remove(t);
    }

    public void CleanNulls()
    {
        detected.RemoveWhere(t => t == null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValid(other)) return;
        detected.Add(other.transform);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValid(other)) return;
        detected.Remove(other.transform);
    }

    private bool IsValid(Collider2D col)
    {
        if (col == null) return false;
        return ((1 << col.gameObject.layer) & detectLayers) != 0;
    }
}
}
