using UnityEngine;
using UnityEngine.AI;
using Game.Combat;

namespace Game.Units
{

/// <summary>
/// Fauna salvaje territorial. Patrulla aleatoriamente. Si una presa entra en
/// su radio de deteccion, la persigue y la ataca. Sin lograr la matanza, persigue
/// hasta que la presa sale del rango. Al perder objetivo, vuelve a wander.
///
/// A diferencia del Bear, no come carne (no es comportamiento omnivoro).
/// </summary>
public abstract class AggressiveWildlife : NPC
{
    private enum WildState { Wander, Chase, Attack }

    [Header("Detection")]
    [SerializeField] protected float detectionRange = 4f;
    [SerializeField] protected float attackRange = 1.3f;
    [Tooltip("Layers de presas validas. Tipicamente Sheep + AllyHitbox + EnemyHitbox.")]
    [SerializeField] protected LayerMask preyLayers;
    [Tooltip("Cambia de objetivo solo si la nueva opcion esta a < distActual * este factor (evita flickering). 0.85 = al menos 15% mas cerca.")]
    [Range(0.1f, 1f)]
    [SerializeField] protected float targetSwitchHysteresis = 0.85f;

    [Header("Attack")]
    [SerializeField] protected int attackDamage = 1;
    [SerializeField] protected float attackCooldown = 1.5f;
    [SerializeField] protected float attackAnimationDuration = 0.7f;
    [Tooltip("Momento dentro de la animacion en el que se aplica el dano.")]
    [SerializeField] protected float attackImpactTime = 0.35f;

    [Header("Wander")]
    [SerializeField] protected float wanderRadius = 5f;
    [SerializeField] protected float wanderIdleMin = 1.5f;
    [SerializeField] protected float wanderIdleMax = 3.5f;

    private WildState state = WildState.Wander;
    private Transform currentPrey;
    private float lastAttackTime = -999f;
    private float nextWanderPickTime = 0f;

    protected override void Start()
    {
        movementType = MovementType.Static;
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;
        switch (state)
        {
            case WildState.Wander: TickWander(); break;
            case WildState.Chase:  TickChase();  break;
            case WildState.Attack: /* Invoke pipeline */ break;
        }
    }

    private void TickWander()
    {
        Transform prey = FindNearestPrey();
        if (prey != null)
        {
            currentPrey = prey;
            state = WildState.Chase;
            return;
        }

        if (Time.time >= nextWanderPickTime &&
            (!navMeshAgent.hasPath || navMeshAgent.remainingDistance < 0.2f))
        {
            Vector3 dest;
            if (TryPickRandomDestination(out dest))
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(dest);
            }
            nextWanderPickTime = Time.time + Random.Range(wanderIdleMin, wanderIdleMax);
        }
    }

    private void TickChase()
    {
        if (currentPrey == null) { ResetToWander(); return; }

        // Reevaluacion con histeresis
        Transform nearestPrey = FindNearestPrey();
        if (nearestPrey != null && nearestPrey != currentPrey)
        {
            float distCurr = Vector2.Distance(transform.position, currentPrey.position);
            float distNew = Vector2.Distance(transform.position, nearestPrey.position);
            if (distNew < distCurr * targetSwitchHysteresis) currentPrey = nearestPrey;
        }

        // Si la presa sale del rango de deteccion (con margen), vuelve a wander
        float distToPrey = Vector2.Distance(transform.position, currentPrey.position);
        if (distToPrey > detectionRange * 1.5f) { ResetToWander(); return; }

        if (distToPrey <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            BeginAttack();
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(currentPrey.position);
    }

    private void BeginAttack()
    {
        state = WildState.Attack;
        navMeshAgent.isStopped = true;
        if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
        lastAttackTime = Time.time;

        if (currentPrey != null)
        {
            float dx = currentPrey.position.x - transform.position.x;
            if (dx > 0.01f) transform.localScale = new Vector3(1f, 1f, 1f);
            else if (dx < -0.01f) transform.localScale = new Vector3(-1f, 1f, 1f);
        }

        if (animator != null) animator.SetTrigger("doAttack");
        OnAttackBegin();
        Invoke(nameof(ApplyAttackDamage), attackImpactTime);
        Invoke(nameof(FinishAttack), attackAnimationDuration);
    }

    private void ApplyAttackDamage()
    {
        if (currentPrey == null) return;
        float dist = Vector2.Distance(transform.position, currentPrey.position);
        if (dist > attackRange * 1.25f) return;

        IDamageReceiver receiver = currentPrey.GetComponentInParent<IDamageReceiver>();
        if (receiver == null) return;

        GameObject targetRoot = ((Component)receiver).gameObject;
        float ratio = receiver.IsStructure ? 1f : CombatHelper.GetMassRatio(gameObject, targetRoot);
        int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * ratio));
        Vector2 dir = currentPrey.position - transform.position;
        receiver.ApplyDamage(scaledDamage, true, false, dir, ratio);
    }

    protected virtual void OnAttackBegin() { }

    private void FinishAttack()
    {
        if (currentPrey == null) { ResetToWander(); return; }
        state = WildState.Chase;
    }

    private Transform FindNearestPrey()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, preyLayers);
        Transform best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;
            if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
            float d = ((Vector2)c.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = c.transform; }
        }
        return best;
    }

    private bool TryPickRandomDestination(out Vector3 dest)
    {
        Vector3 candidate = Random.insideUnitSphere * wanderRadius + transform.position;
        candidate.z = transform.position.z;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidate, out hit, wanderRadius, NavMesh.AllAreas))
        {
            dest = hit.position;
            return true;
        }
        dest = transform.position;
        return false;
    }

    private void ResetToWander()
    {
        currentPrey = null;
        state = WildState.Wander;
        nextWanderPickTime = 0f;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
}
