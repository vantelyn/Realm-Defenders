using UnityEngine;
using UnityEngine.AI;
using Game.Units;

namespace Game.Combat
{

public class DamageReceiverPlayer : MonoBehaviour, IDamageReceiver
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
    private IDamageBlocker blocker;
    private bool knockbackActive;
    private HitFlashEffect hitFlash;
    private NavMeshAgent navAgent;
    private bool knockbackDisabledAgent;
    private bool hasGetHitParam;

    public float forceImpulse = 5;
    [Tooltip("Tiempo de paron tras recibir knockback (segundos)")]
    public float knockbackDuration = 0.3f;
    [Tooltip("Multiplicador del knockback recibido. 1=normal, 0=inmune, 0.3=resiste mucho.")]
    [Range(0f, 1f)]
    public float knockbackReceivedMultiplier = 1f;

    /// <summary>Disparado tras aplicar dano efectivo (post-block).</summary>
    public event System.Action<int, Vector2> OnDamaged;
    public event System.Action OnDying;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAtFullHealth => currentHealth >= maxHealth;
    public float HealthRatio => maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
    public bool IsStructure => false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        rb2D = GetComponent<Rigidbody2D>();
        playerUnit = GetComponent<PlayerUnit>();
        blocker = GetComponent<IDamageBlocker>();
        hitFlash = GetComponent<HitFlashEffect>();
        navAgent = GetComponent<NavMeshAgent>();
        hasGetHitParam = AnimatorHasParam(animator, "getHit");
    }

    public void ApplyDamage(int amount, bool applyForce, bool applyHitAnimation, Vector2 hitDirection, float forceMultiplier = 1f)
    {

        float damageMult = (blocker != null) ? Mathf.Clamp01(blocker.GetDamageMultiplier(hitDirection)) : 1f;
        bool blocked = damageMult <= 0f;
        int effectiveAmount = blocked ? 0 : Mathf.Max(1, Mathf.RoundToInt(amount * damageMult));

        if (!blocked)
        {
            currentHealth -= effectiveAmount;
            if (hitFlash != null) hitFlash.Flash();
            if (OnDamaged != null) OnDamaged(effectiveAmount, hitDirection);
        }
        float effectiveKnockback = forceImpulse * forceMultiplier * knockbackReceivedMultiplier;
        bool canKnockback = playerUnit == null || !playerUnit.IsGarrisoned;
        if (applyForce && effectiveKnockback > 0.001f && canKnockback)
        {
            if (playerUnit != null)
            {
                playerUnit.canMove = false;
                // Cancela el ataque en curso: el knockback nos saca de la pose y
                // queremos que la IA reevalue distancia en el siguiente frame.
                // El animation event DetectAndDamageTargets posterior chequea
                // isAttacking y hara early-return.
                if (playerUnit.IsAttacking) playerUnit.EndAttack();
            }

            if (navAgent != null && navAgent.enabled)
            {
                navAgent.enabled = false;
                knockbackDisabledAgent = true;
            }

            if (rb2D.bodyType == RigidbodyType2D.Kinematic)
            {
                rb2D.bodyType = RigidbodyType2D.Dynamic;
                knockbackActive = true;
            }

            rb2D.AddForce(hitDirection.normalized * effectiveKnockback, ForceMode2D.Impulse);
            CancelInvoke(nameof(ResetMovement));
            Invoke(nameof(ResetMovement), knockbackDuration);
        }
        if (applyHitAnimation && !blocked && animator != null && hasGetHitParam)
        {
            animator.SetTrigger("getHit");
        }
        if (!blocked && currentHealth <= 0)
        {
            if (OnDying != null) OnDying();
            DropItem();
            GoToHell();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
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
        if (knockbackActive)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.bodyType = RigidbodyType2D.Kinematic;
            knockbackActive = false;
        }

        if (knockbackDisabledAgent && navAgent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            navAgent.enabled = true;
            knockbackDisabledAgent = false;
        }

        if (playerUnit != null && !playerUnit.IsBusy) playerUnit.canMove = true;
    }

    void GoToHell()
    {
        Destroy(gameObject);
    }

    private static bool AnimatorHasParam(Animator a, string paramName)
    {
        if (a == null || a.runtimeAnimatorController == null) return false;
        var ps = a.parameters;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].name == paramName) return true;
        }
        return false;
    }
}
}
