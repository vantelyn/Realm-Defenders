using UnityEngine;
using System.Collections.Generic;
using Game.Combat;

namespace Game.Projectiles
{

/// <summary>
/// Base para proyectiles lanzados por enemigos. Encapsula la logica comun:
/// vuelo en arco hacia un worldTarget (Launch), detect de impacto contra
/// hitLayers, aplicacion de dano via IDamageReceiver, y aterrizaje con
/// destruccion diferida si no impactara con nada.
///
/// Las subclases solo implementan OrientSprite(dir) para decidir como se
/// orienta el sprite durante el vuelo (rotacion del transform vs flip
/// horizontal del child vs anim de rotacion en bucle, etc).
/// </summary>
public abstract class ThrownProjectileEnemy : MonoBehaviour
{
    [Header("Vuelo")]
    [SerializeField] protected float speed = 8f;
    [SerializeField] protected float baseArcHeight = 0.8f;
    [SerializeField] protected float arcReferenceSpeed = 8f;

    [Header("Referencias")]
    [SerializeField] protected Transform spriteChild;

    [Header("Impacto")]
    [SerializeField] protected LayerMask hitLayers;
    [SerializeField] protected int damage = 1;
    [SerializeField] protected float hitCheckRadius = 0.2f;

    [Header("Lifetime")]
    [SerializeField] protected float timeToDestroyAfterLanding = 2f;

    protected Vector3 startPos;
    protected Vector3 targetPos;
    protected float flightDuration;
    protected float arcHeight;
    protected float elapsed;
    protected bool launched;
    protected bool landed;
    protected Vector2 flightDir = Vector2.right;

    public void Launch(Vector3 worldTarget)
    {
        startPos = transform.position;
        targetPos = worldTarget;
        targetPos.z = startPos.z;

        Vector3 delta = targetPos - startPos;
        float distance = delta.magnitude;
        flightDir = distance > 0.0001f ? (Vector2)(delta / distance) : Vector2.right;

        flightDuration = distance / Mathf.Max(speed, 0.0001f);
        arcHeight = baseArcHeight * (arcReferenceSpeed / Mathf.Max(speed, 0.0001f));

        elapsed = 0f;
        launched = true;
        landed = false;

        OrientSprite(flightDir);
    }

    protected virtual void Update()
    {
        if (!launched || landed) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightDuration);

        transform.position = Vector3.Lerp(startPos, targetPos, t);

        if (spriteChild != null)
        {
            float h = Mathf.Sin(t * Mathf.PI) * arcHeight;
            Vector3 lp = spriteChild.localPosition;
            lp.y = h;
            spriteChild.localPosition = lp;
        }

        if (CheckHit()) return;

        if (t >= 1f) Land();
    }

    /// <summary>Cada subclase decide como orientar el sprite segun la direccion de vuelo.</summary>
    protected abstract void OrientSprite(Vector2 dir);

    /// <summary>Hook llamado al impactar (CheckHit con colliders) o aterrizar (Land sin colliders).
    /// Default no-op. Las subclases pueden override para spawn de efectos como explosiones AOE.</summary>
    protected virtual void OnImpact(Vector3 worldPos) { }

    protected virtual bool CheckHit()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(hitLayers);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        List<Collider2D> hits = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, hitCheckRadius, filter, hits);

        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            if (hit == null) continue;

            Vector2 hitDirection = hit.transform.position - transform.position;

            IDamageReceiver receiver = hit.GetComponentInParent<IDamageReceiver>();
            if (receiver != null && damage > 0)
            {
                // applyHitAnimation=false porque los aliados no tienen anim "getHit".
                receiver.ApplyDamage(damage, true, false, hitDirection);
            }

            OnImpact(transform.position);
            Destroy(gameObject);
            return true;
        }
        return false;
    }

    protected virtual void Land()
    {
        landed = true;
        if (spriteChild != null)
        {
            Vector3 lp = spriteChild.localPosition;
            lp.y = 0f;
            spriteChild.localPosition = lp;
        }
        OnImpact(transform.position);
        Destroy(gameObject, timeToDestroyAfterLanding);
    }
}
}