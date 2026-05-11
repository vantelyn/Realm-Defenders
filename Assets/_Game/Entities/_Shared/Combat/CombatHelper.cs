using UnityEngine;

namespace Game.Combat
{

/// <summary>
/// Utilidades de combate. Sistema de masa basado en el area del Collider2D del
/// child "Hitbox": si dos unidades se enfrentan, la mas grande inflige mas
/// danio y mas knockback. Edificios, arboles y recursos no participan (no
/// tienen child "Hitbox"), por lo que el ratio queda en 1.
/// </summary>
public static class CombatHelper
{
    /// <summary>Calcula la masa de un GameObject a partir del area del Collider2D de su child "Hitbox". Devuelve 0 si no lo tiene.</summary>
    public static float GetMassFromHitbox(GameObject go)
    {
        if (go == null) return 0f;
        Transform hb = go.transform.Find("Hitbox");
        if (hb == null) return 0f;
        Collider2D col = hb.GetComponent<Collider2D>();
        if (col == null) return 0f;
        Vector2 sz = col.bounds.size;
        return Mathf.Max(0.001f, sz.x * sz.y);
    }

    /// <summary>
    /// Ratio attackerMass/targetMass clampeado a [minRatio, maxRatio]. Default [0.25, 4].
    /// Si alguno de los GameObjects no tiene Hitbox (estructuras, arboles, etc.), devuelve 1f.
    /// </summary>
    public static float GetMassRatio(GameObject attacker, GameObject target, float minRatio = 0.25f, float maxRatio = 4f)
    {
        float mAtk = GetMassFromHitbox(attacker);
        float mTgt = GetMassFromHitbox(target);
        if (mAtk <= 0f || mTgt <= 0f) return 1f;
        return Mathf.Clamp(mAtk / mTgt, minRatio, maxRatio);
    }
}
}
