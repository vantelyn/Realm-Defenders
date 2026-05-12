using UnityEngine;

namespace Game.Projectiles
{

/// <summary>
/// Proyectil magico del Shaman. Su sprite vive en spriteChild con Animator
/// reproduciendo Proyectile_Idle (loop de rotacion), asi que aqui solo flip
/// horizontal. damage=0 en el prefab: todo el dano lo aplica ShamanExplosion
/// (AOE) que se spawnea en OnImpact.
/// </summary>
public class ShamanProjectile : ThrownProjectileEnemy
{
    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;

    protected override void OrientSprite(Vector2 dir)
    {
        if (spriteChild == null) return;
        Vector3 s = spriteChild.localScale;
        s.x = Mathf.Abs(s.x) * (dir.x < 0 ? -1f : 1f);
        spriteChild.localScale = s;
    }

    protected override void OnImpact(Vector3 worldPos)
    {
        if (explosionPrefab == null) return;
        GameObject ex = Instantiate(explosionPrefab, worldPos, Quaternion.identity);
        ShamanExplosion script = ex.GetComponent<ShamanExplosion>();
        if (script != null) script.Detonate();
    }

    /// <summary>Override para destruir el proyectil inmediato al aterrizar
    /// (sin el delayed destroy del default) y que la explosion sea limpia
    /// visualmente sin el proyectil sigueindo tumbado en el suelo.</summary>
    protected override void Land()
    {
        landed = true;
        if (spriteChild != null)
        {
            Vector3 lp = spriteChild.localPosition;
            lp.y = 0f;
            spriteChild.localPosition = lp;
        }
        OnImpact(transform.position);
        Destroy(gameObject);
    }
}
}
