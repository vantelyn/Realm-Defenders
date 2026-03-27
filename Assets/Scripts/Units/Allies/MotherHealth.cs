using UnityEngine;
using UnityEngine.UI;

public class MotherHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Barra de vida")]
    public Image fillImage;


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
        float percent = currentHealth / maxHealth;
        fillImage.fillAmount = percent;
    }

    private void Die()
    {
        Debug.Log("¡El edificio ha sido destruido!");
        // Aquí pondrás tu lógica de Game Over
    }

    void OnValidate()
    {
        if (fillImage != null)
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            UpdateBar();
        }
    }

}