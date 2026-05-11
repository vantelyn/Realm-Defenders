namespace Game.Combat
{

/// <summary>
/// Contrato comun para todo lo que puede recibir dano: unidades aliadas,
/// enemigas, edificios, recursos. Permite a los atacantes resolver el target
/// con una sola llamada en lugar de cascadear por tipos concretos.
/// </summary>
public interface IDamageReceiver
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    float HealthRatio { get; }
    bool IsAtFullHealth { get; }

    /// <summary>
    /// True para edificios y demas estructuras estaticas; false para unidades
    /// y recursos vivos. Los atacantes lo usan para escalar el dano
    /// (units vs buildings) sin necesidad de checks de tipo.
    /// </summary>
    bool IsStructure { get; }

    void ApplyDamage(int amount, bool applyForce, bool applyHitAnimation, UnityEngine.Vector2 hitDirection);
}

}
