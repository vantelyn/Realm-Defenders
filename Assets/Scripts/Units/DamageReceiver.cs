using UnityEngine;

public class DamageReceiver : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 1;
    public int currentHealth;

    [Header("Drop")]
    public GameObject[] itemsToDrop;
    private Rigidbody2D rb2D;
    private Animator animator;
    public float forceImpulse = 5;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        rb2D = GetComponent<Rigidbody2D>();
    }


    public void ApplyDamage(int amount, bool applyForce, bool applyHitAnimation, Vector2 hitDirection)
    {
        currentHealth -= amount;

        if (applyForce)
        {
            rb2D.bodyType = RigidbodyType2D.Dynamic;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.AddForce(hitDirection.normalized * forceImpulse, ForceMode2D.Impulse);
            Invoke("ReturnToKinematic", 0.2f);
        }

        if (applyHitAnimation)
        {
            animator.SetTrigger("getHit");
        }

        if (currentHealth<=0)
        {
            DropItem();
            GoToHell();
        }
    }

    void ReturnToKinematic()
    {
        rb2D.linearVelocity = Vector2.zero;
        rb2D.bodyType = RigidbodyType2D.Kinematic;
    }

    void DropItem()
    {
        for (int i = 0; i < itemsToDrop.Length; i++)
        {
            Instantiate(itemsToDrop[i], transform.position, Quaternion.identity);
            
        }
    }

    void GoToHell()
    {
        Destroy(gameObject);
    }
}
