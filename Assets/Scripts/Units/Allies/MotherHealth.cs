using UnityEngine;
using UnityEngine.UI;

public class MotherHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Barra de vida")]
    public Image fillImage;

    [Header("Destroyed Base")]
    public GameObject destroyedBasePrefab;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateBar();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateBar();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdateBar()
    {
        if (fillImage != null)
        {
            float percent = currentHealth / maxHealth;
            fillImage.fillAmount = percent;
        }
    }

    private void Die()
    {
        SpawnDestroyedBase();
        GoToHell();
    }

    void SpawnDestroyedBase()
    {
        if (destroyedBasePrefab != null)
        {
            Instantiate(destroyedBasePrefab, transform.position, transform.rotation);
        }
    }

    void GoToHell()
    {
        Destroy(gameObject);
    }

    void OnValidate()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (fillImage != null)
        {
            UpdateBar();
        }
    }
}