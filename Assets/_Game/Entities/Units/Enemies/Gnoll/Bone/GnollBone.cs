using UnityEngine;

namespace Game.Projectiles
{

/// <summary>
/// Hueso lanzado por el Gnoll. El sprite vive en spriteChild con Animator
/// reproduciendo Bone_Idle (loop de rotacion), asi que aqui solo aplicamos
/// flip horizontal segun direccion de vuelo.
/// </summary>
public class GnollBone : ThrownProjectileEnemy
{
    protected override void OrientSprite(Vector2 dir)
    {
        if (spriteChild == null) return;
        Vector3 s = spriteChild.localScale;
        s.x = Mathf.Abs(s.x) * (dir.x < 0 ? -1f : 1f);
        spriteChild.localScale = s;
    }
}
}
