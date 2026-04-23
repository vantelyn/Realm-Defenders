using UnityEngine;

public class DamageReceiverPlayer : MonoBehaviour
{
    [System.Serializable]
    public class DroppableItem
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float dropChance = 1f;
    }

    [Header("Stats")]
    public int maxHealth = 1;
    public int currentHealth;

    [Header("Drop")]
    public DroppableItem[] itemsToDrop;
    public float dropRadius = 0.5f;

    private Rigidbody2D rb2D;
    private Animator animator;
    private PlayerUnit playerUnit;
    public float forceImpulse = 5;
    private IDamageBlocker blocker;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        rb2D = GetComponent<Rigidbody2D>();
        playerUnit = GetComponent<PlayerUnit>();
        blocker = GetComponent<IDamageBlocker>();
    }

    public void ApplyDamage(int amount, bool applyForce, bool applyHitAnimation, Vector2 hitDirection)
    {
        if (blocker != null && blocker.TryBlock(hitDirection))
        {
            return; // golpe bloqueado
        }

        currentHealth -= amount;

        if (applyForce)
        {
            if (playerUnit != null) playerUnit.canMove = false;
            rb2D.AddForce(hitDirection.normalized * forceImpulse, ForceMode2D.Impulse);
            Invoke(nameof(ResetMovement), 0.1f);
        }

        if (applyHitAnimation)
        {
            animator.SetTrigger("getHit");
        }

        if (currentHealth <= 0)
        {
            DropItem();
            GoToHell();
        }
    }

    void DropItem()
    {
        foreach (var item in itemsToDrop)
        {
            if (item.prefab == null) continue;
            if (Random.value > item.dropChance) continue;

            Vector3 spawnPos;
            if (item.prefab.CompareTag("Stump"))
            {
                spawnPos = transform.position;
            }
            else
            {
                Vector2 offset = Random.insideUnitCircle * dropRadius;
                spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);
            }
            Instantiate(item.prefab, spawnPos, Quaternion.identity);
        }
    }

    void ResetMovement()
    {
        if (playerUnit != null) playerUnit.canMove = true;
    }

    void GoToHell()
    {
        Destroy(gameObject);
    }
}