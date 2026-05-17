using UnityEngine;
using UnityEngine.AI;

namespace Game.Combat
{

public class DamageReceiver : MonoBehaviour, IDamageReceiver
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

    [Header("Death")]
    [Tooltip("Si false, un listener de OnDying se encarga de destruir el GameObject (p.ej. tras una animacion de muerte).")]
    public bool autoDestroyOnDeath = true;

    [Header("Drop")]
    public DroppableItem[] itemsToDrop;
    public float dropRadius = 0.5f;

    private Rigidbody2D rb2D;
    private Animator animator;
    private HitFlashEffect hitFlash;
    private NavMeshAgent navAgent;
    private IDamageBlocker blocker;
    private bool knockbackDisabledAgent;
    private bool hasGetHitParam;
    public float forceImpulse = 5;
    [Tooltip("Tiempo de paron tras recibir knockback (segundos)")]
    public float knockbackDuration = 0.3f;
    [Tooltip("Multiplicador del knockback recibido. 1=normal, 0=inmune, 0.3=resiste mucho.")]
    [Range(0f, 1f)]
    public float knockbackReceivedMultiplier = 1f;

    /// <summary>Disparado una unica vez cuando HP llega a 0, antes del drop y antes del Destroy.</summary>
    public event System.Action OnDying;

    /// <summary>Disparado tras aplicar dano efectivo (post-block). Util para efectos visuales
    /// como wobble en arboles, particulas, sonido, etc.</summary>
    public event System.Action<int, Vector2> OnDamaged;
    private bool dyingFired;

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
        hitFlash = GetComponent<HitFlashEffect>();
        navAgent = GetComponent<NavMeshAgent>();
        blocker = GetComponent<IDamageBlocker>();
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
        if (applyForce && effectiveKnockback > 0.001f && rb2D != null)
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.enabled = false;
                knockbackDisabledAgent = true;
            }
            rb2D.bodyType = RigidbodyType2D.Dynamic;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.AddForce(hitDirection.normalized * effectiveKnockback, ForceMode2D.Impulse);
            CancelInvoke(nameof(ReturnToKinematic));
            Invoke(nameof(ReturnToKinematic), knockbackDuration);
        }

        if (applyHitAnimation && !blocked && animator != null && hasGetHitParam)
        {
            animator.SetTrigger("getHit");
        }

        if (!blocked && currentHealth <= 0 && !dyingFired)
        {
            dyingFired = true;
            if (OnDying != null) OnDying();
            DropItem();
            if (autoDestroyOnDeath) GoToHell();
        }
    }

    void ReturnToKinematic()
    {
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.bodyType = RigidbodyType2D.Kinematic;
        }

        if (knockbackDisabledAgent && navAgent != null)
        {
            // Reanclar al NavMesh por si la fuerza nos saco del mismo
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            navAgent.enabled = true;
            knockbackDisabledAgent = false;
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
