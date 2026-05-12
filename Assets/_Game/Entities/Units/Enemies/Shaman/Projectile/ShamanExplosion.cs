using UnityEngine;
using System.Collections.Generic;
using Game.Combat;

namespace Game.Projectiles
{

/// <summary>
/// Explosion del proyectil del Shaman. Se instancia en el punto de impacto
/// del proyectil. Detonate() aplica dano AOE a todos los IDamageReceiver
/// dentro del radio cuya layer pertenezca a hitLayers. El GameObject se
/// auto-destruye tras 'duration' (debe coincidir con la longitud del clip
/// Proyectile_Explosion para que la animacion se complete).
/// </summary>
public class ShamanExplosion : MonoBehaviour
{
    [Header("AOE")]
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private int damage = 2;
    [SerializeField] private LayerMask hitLayers;

    [Header("Lifetime")]
    [Tooltip("Tiempo antes de destruir el GO. Debe ser >= longitud del clip de explosion.")]
    [SerializeField] private float duration = 0.9f;

    public void Detonate()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(hitLayers);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        List<Collider2D> hits = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, radius, filter, hits);

        // Dedup por receiver root: una unit puede tener varios colliders en hitLayers
        // (root + Hitbox child), y queremos aplicarle dano solo una vez.
        HashSet<IDamageReceiver> applied = new HashSet<IDamageReceiver>();
        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            if (hit == null) continue;
            IDamageReceiver receiver = hit.GetComponentInParent<IDamageReceiver>();
            if (receiver == null) continue;
            if (applied.Contains(receiver)) continue;
            applied.Add(receiver);
            Vector2 dir = hit.transform.position - transform.position;
            receiver.ApplyDamage(damage, true, false, dir);
        }
        Destroy(gameObject, duration);
    }
}
}
