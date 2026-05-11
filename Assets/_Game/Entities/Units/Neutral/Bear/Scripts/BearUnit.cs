using UnityEngine;
using UnityEngine.AI;
using Game.Combat;

namespace Game.Units
{

/// <summary>
/// Oso neutral. Patrulla el mapa (wandering) buscando carne en el suelo.
/// Si encuentra carne, va a comerla: se para junto a ella, 1s despues la
/// destruye y recupera +3 HP, con cooldown antes de poder comer otra.
/// Si detecta una presa (oveja, unidad aliada o enemiga) y no hay carne
/// prioritaria, la persigue y la ataca.
/// </summary>
public class BearUnit : NPC
{
    private enum BearState { Wander, SeekFood, Eating, Chase, Attack }

    [Header("Detection")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackRange = 1.2f;
    [Tooltip("Layers de presas validas: Sheep + PlayerUnit + EnemyUnit")]
    [SerializeField] private LayerMask preyLayers;
    [Tooltip("Layers donde puede haber carne (normalmente Resources)")]
    [SerializeField] private LayerMask foodLayers;
    [SerializeField] private string foodTag = "Meat";
    [Tooltip("Cambia de objetivo solo si la nueva opcion esta a < distActual * este factor (evita flickering entre objetivos equidistantes). 0.85 = al menos 15% mas cerca.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float targetSwitchHysteresis = 0.85f;

    [Header("Attack")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationDuration = 0.9f;
    [Tooltip("Momento dentro de la animacion en el que se aplica el dano")]
    [SerializeField] private float attackImpactTime = 0.45f;

    [Header("Eat")]
    [SerializeField] private int eatHealAmount = 3;
    [SerializeField] private float eatDuration = 1f;
    [SerializeField] private float eatCooldown = 1f;
    [SerializeField] private float eatReachDistance = 0.3f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderIdleMin = 1.5f;
    [SerializeField] private float wanderIdleMax = 3.5f;

    private BearState state = BearState.Wander;
    private Transform currentPrey;
    private Transform currentFood;
    private float lastAttackTime = -999f;
    private float nextEatAllowed = 0f;
    private float nextWanderPickTime = 0f;
    private DamageReceiver damageReceiver;

    protected override void Start()
    {
        // Forzamos Static para que NPC no inicie ninguna rutina; el movimiento
        // lo gestionamos manualmente con la state machine de abajo.
        movementType = MovementType.Static;
        base.Start();
        damageReceiver = GetComponent<DamageReceiver>();
    }

    protected override void Update()
    {
        // base.Update() actualiza animacion isRunning y flip por velocity
        base.Update();

        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;

        switch (state)
        {
            case BearState.Wander:   TickWander();   break;
            case BearState.SeekFood: TickSeekFood(); break;
            case BearState.Eating:   TickEating();   break;
            case BearState.Chase:    TickChase();    break;
            case BearState.Attack:   /* gestionado por Invoke */ break;
        }
    }

    // ---------- Wander ----------
    private void TickWander()
    {
        if (TryAcquireFoodOrPrey()) return;

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

    // ---------- Adquisicion de objetivo ----------
    /// <summary>
    /// Prioridad: carne (si fuera de cooldown) > presa.
    /// Devuelve true si cambio de estado.
    /// </summary>
    private bool TryAcquireFoodOrPrey()
    {
        if (Time.time >= nextEatAllowed)
        {
            Transform food = FindNearestFood();
            if (food != null)
            {
                currentFood = food;
                currentPrey = null;
                state = BearState.SeekFood;
                return true;
            }
        }

        Transform prey = FindNearestPrey();
        if (prey != null)
        {
            currentPrey = prey;
            currentFood = null;
            state = BearState.Chase;
            return true;
        }
        return false;
    }

    private Transform FindNearestFood()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, foodLayers);
        Transform best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;
            if (!c.CompareTag(foodTag)) continue;
            float d = ((Vector2)c.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = c.transform; }
        }
        return best;
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
            if (c.transform == transform) continue; // por si acaso
            float d = ((Vector2)c.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = c.transform; }
        }
        return best;
    }

    // ---------- Seek food / Eating ----------
    private void TickSeekFood()
    {
        if (currentFood == null) { ResetToWander(); return; }

        // Reevaluacion: cambiar a carne mas cercana si lo es significativamente (histeresis)
        Transform nearestFood = FindNearestFood();
        if (nearestFood != null && nearestFood != currentFood)
        {
            float distCurr = Vector2.Distance(transform.position, currentFood.position);
            float distNew = Vector2.Distance(transform.position, nearestFood.position);
            if (distNew < distCurr * targetSwitchHysteresis)
            {
                currentFood = nearestFood;
            }
        }

        float dist = Vector2.Distance(transform.position, currentFood.position);
        if (dist <= eatReachDistance)
        {
            navMeshAgent.isStopped = true;
            if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
            state = BearState.Eating;
            Invoke(nameof(FinishEating), eatDuration);
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(currentFood.position);
    }

    private void TickEating()
    {
        // Si otro destruyo la carne mientras comiamos, abortamos sin penalizar cooldown
        if (currentFood == null)
        {
            CancelInvoke(nameof(FinishEating));
            ResetToWander();
        }
    }

    private void FinishEating()
    {
        if (currentFood != null)
        {
            Destroy(currentFood.gameObject);
            if (damageReceiver != null)
            {
                int newHp = Mathf.Min(damageReceiver.currentHealth + eatHealAmount, damageReceiver.maxHealth);
                damageReceiver.currentHealth = newHp;
            }
        }
        currentFood = null;
        nextEatAllowed = Time.time + eatCooldown;
        ResetToWander();
    }

    // ---------- Chase / Attack ----------
    private void TickChase()
    {
        if (currentPrey == null) { ResetToWander(); return; }

        // Reevaluacion: si aparece carne y estamos fuera de cooldown, prioridad carne
        if (Time.time >= nextEatAllowed)
        {
            Transform food = FindNearestFood();
            if (food != null)
            {
                currentFood = food;
                currentPrey = null;
                state = BearState.SeekFood;
                return;
            }
        }

        // Reevaluacion: cambiar a presa mas cercana si lo es significativamente (histeresis)
        Transform nearestPrey = FindNearestPrey();
        if (nearestPrey != null && nearestPrey != currentPrey)
        {
            float distCurr = Vector2.Distance(transform.position, currentPrey.position);
            float distNew = Vector2.Distance(transform.position, nearestPrey.position);
            if (distNew < distCurr * targetSwitchHysteresis)
            {
                currentPrey = nearestPrey;
            }
        }

        float dist = Vector2.Distance(transform.position, currentPrey.position);
        if (dist <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            BeginAttack();
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(currentPrey.position);
    }

    private void BeginAttack()
    {
        state = BearState.Attack;
        navMeshAgent.isStopped = true;
        if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
        lastAttackTime = Time.time;

        // Facing al objetivo
        if (currentPrey != null)
        {
            float dx = currentPrey.position.x - transform.position.x;
            if (dx > 0.01f) transform.localScale = new Vector3(1f, 1f, 1f);
            else if (dx < -0.01f) transform.localScale = new Vector3(-1f, 1f, 1f);
        }

        if (animator != null) animator.SetTrigger("doAttack");

        Invoke(nameof(ApplyAttackDamage), attackImpactTime);
        Invoke(nameof(FinishAttack), attackAnimationDuration);
    }

    private void ApplyAttackDamage()
    {
        if (currentPrey == null) return;
        float dist = Vector2.Distance(transform.position, currentPrey.position);
        if (dist > attackRange * 1.25f) return; // se alejo demasiado

        IDamageReceiver receiver = currentPrey.GetComponentInParent<IDamageReceiver>();
        if (receiver == null) return;

        GameObject targetRoot = ((Component)receiver).gameObject;
        float ratio = receiver.IsStructure ? 1f : CombatHelper.GetMassRatio(gameObject, targetRoot);
        int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * ratio));
        Vector2 dir = currentPrey.position - transform.position;
        receiver.ApplyDamage(scaledDamage, true, false, dir, ratio);
    }

    private void FinishAttack()
    {
        if (currentPrey == null)
        {
            ResetToWander();
            return;
        }
        // Vuelta a chase; si sigue en rango el proximo Tick reentra Attack al cumplirse cooldown
        state = BearState.Chase;
    }

    // ---------- Helpers ----------
    private void ResetToWander()
    {
        currentPrey = null;
        currentFood = null;
        state = BearState.Wander;
        nextWanderPickTime = 0f; // permite re-pick inmediato
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
}
