using UnityEngine;

namespace Game.Projectiles
{

/// <summary>
/// Arpon lanzado por HarpoonFish. Sprite estatico, asi que la orientacion
/// se hace rotando el transform raiz segun la direccion de vuelo (estilo
/// flecha). El spriteChild se eleva por el arco en la base, no se toca aqui.
/// </summary>
public class Harpoon : ThrownProjectileEnemy
{
    protected override void OrientSprite(Vector2 dir)
    {
        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
    }
}
}
