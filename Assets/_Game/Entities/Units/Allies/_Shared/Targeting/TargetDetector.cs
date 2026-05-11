using System.Collections.Generic;
using UnityEngine;

namespace Game.Targeting
{

public class TargetDetector : MonoBehaviour
{
    [SerializeField] private LayerMask detectLayers;
    [Tooltip("Intervalo de poll para descubrir colliders que aparezcan dentro del trigger sin disparar OnTriggerEnter2D (p.ej. Stumps spawneados al matar un arbol).")]
    [SerializeField] private float pollInterval = 0.5f;

    private readonly HashSet<Transform> detected = new HashSet<Transform>();
    private CircleCollider2D triggerCol;
    private float pollTimer;

    public IReadOnlyCollection<Transform> Detected => detected;

    private void Awake()
    {
        triggerCol = GetComponent<CircleCollider2D>();
    }

    private void Start()
    {
        Poll();
    }

    private void Update()
    {
        pollTimer -= Time.deltaTime;
        if (pollTimer <= 0f)
        {
            pollTimer = pollInterval;
            Poll();
        }
    }

    private void Poll()
    {
        if (triggerCol == null) return;
        Vector2 center = (Vector2)transform.position + triggerCol.offset;
        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        float radius = triggerCol.radius * scale;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, detectLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            detected.Add(hits[i].transform);
        }
    }

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
