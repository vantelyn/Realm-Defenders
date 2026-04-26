using UnityEngine;
using System.Collections.Generic;
using Game.Combat;

namespace Game.Projectiles
{

public class Arrow : MonoBehaviour
{
    [Header("Vuelo")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float baseArcHeight = 0.6f;
    [SerializeField] private float arcReferenceSpeed = 12f; // a esta velocidad el arco vale baseArcHeight

    [Header("Referencias")]
    [SerializeField] private Transform spriteChild;

    [Header("Impacto")]
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCheckRadius = 0.15f;

    [Header("Lifetime")]
    [SerializeField] private float timeToDestroyAfterLanding = 3f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float flightDuration;
    private float arcHeight;
    private float elapsed;
    private bool launched;
    private bool landed;

    public void Launch(Vector3 worldTarget)
    {
        startPos = transform.position;
        targetPos = worldTarget;
        targetPos.z = startPos.z;

        Vector3 delta = targetPos - startPos;
        float distance = delta.magnitude;
        Vector2 dir = distance > 0.0001f ? (Vector2)(delta / distance) : Vector2.right;

        flightDuration = distance / Mathf.Max(speed, 0.0001f);

        // Arco inversamente proporcional a la velocidad
        arcHeight = baseArcHeight * (arcReferenceSpeed / Mathf.Max(speed, 0.0001f));

        elapsed = 0f;
        launched = true;
        landed = false;

        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
    }

    void Update()
    {
        if (!launched || landed) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightDuration);

        transform.position = Vector3.Lerp(startPos, targetPos, t);

        if (spriteChild != null)
        {
            float h = Mathf.Sin(t * Mathf.PI) * arcHeight;
            spriteChild.position = transform.position + Vector3.up * h;
        }

        if (CheckHit()) return;

        if (t >= 1f) Land();
    }

    private bool CheckHit()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(hitLayers);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        List<Collider2D> hits = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, hitCheckRadius, filter, hits);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            Vector2 hitDirection = hit.transform.position - transform.position;

            DamageReceiver receiver = hit.GetComponent<DamageReceiver>();
            if (receiver != null)
            {
                int layer = hit.gameObject.layer;
                bool isEnemy = layer == LayerMask.NameToLayer("Enemy") ||
                               layer == LayerMask.NameToLayer("Sheep");
                bool isTree = layer == LayerMask.NameToLayer("Tree");
                receiver.ApplyDamage(damage, isEnemy, isTree, hitDirection);
            }

            Destroy(gameObject);
            return true;
        }
        return false;
    }

    private void Land()
    {
        landed = true;
        if (spriteChild != null) spriteChild.localPosition = Vector3.zero;
        Destroy(gameObject, timeToDestroyAfterLanding);
    }
}
}
