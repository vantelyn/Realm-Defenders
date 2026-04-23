using UnityEngine;

/// <summary>
/// Receptor de daño para edificios del jugador. No aplica fuerzas (los edificios
/// no se mueven) ni animación de golpe. Al morir destruye el GameObject, lo que
/// automáticamente desregistra cualquier StrategicTarget asociado.
/// </summary>
public class DamageReceiverBuilding : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 200;
    public int currentHealth;

    [Header("Destruction")]
    [SerializeField] private GameObject destroyedPrefab;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAtFullHealth => currentHealth >= maxHealth;
    public float HealthRatio => maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;

    public event System.Action<int, int> OnHealthChanged;

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ApplyDamage(int amount, bool _, bool __, Vector2 ___)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (destroyedPrefab != null)
        {
            Instantiate(destroyedPrefab, transform.position, transform.rotation);
        }
        Destroy(gameObject);
    }
}