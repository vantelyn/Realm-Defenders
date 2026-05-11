using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Game.Combat;
using Game.Targeting;

namespace Game.AI
{

/// <summary>
/// Base abstracta para IAs enemigas. Gestiona el targeting (omnisciente +
/// oportunidad), el movimiento via NavMeshAgent, el ciclo de ataque y la
/// limpieza de detectados. Las subclases implementan el ataque concreto.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public abstract class BaseEnemyAI : MonoBehaviour
{
    public enum AIState { Idle, SeekStrategic, ChaseOpportunity, Attack }

    [Header("Attack")]
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float attackCooldown = 2f;
    [SerializeField] protected float attackAnimationDuration = 0.5f;
    [SerializeField] protected LayerMask targetLayer;

    [Header("Movement")]
    [SerializeField] protected float stopDistanceStrategic = 0.5f;
    [SerializeField] protected float stopDistanceOpportunity = 1.5f;

    [Header("Repath")]
    [SerializeField] protected float repathInterval = 0.25f;

    protected NavMeshAgent agent;
    protected Animator animator;

    private readonly List<Transform> detectedUnits = new List<Transform>();

    protected Transform currentOpportunityTarget;
    protected StrategicTarget currentStrategicTarget;

    protected AIState state = AIState.Idle;
    protected Vector2 attackDirectionVector;
    protected float lastAttackTime = -999f;
    protected float lastRepathTime = -999f;
    protected bool isAttacking;
    protected bool canMove = true;

    public AIState State => state;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    protected virtual void Update()
    {
        if (isAttacking) return;

        CleanDetectedUnits();
        PickOpportunityTarget();

        if (currentOpportunityTarget != null)
        {
            TickOpportunity();
        }
        else
        {
            TickStrategic();
        }

        UpdateAnimationAndFacing();
    }

    protected virtual void FixedUpdate()
    {
        if (agent != null) agent.isStopped = !canMove;
    }

    // ----- Targeting -----

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidOpportunityTarget(other)) return;
        if (!detectedUnits.Contains(other.transform))
        {
            detectedUnits.Add(other.transform);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;
        detectedUnits.Remove(other.transform);
        if (currentOpportunityTarget == other.transform)
        {
            currentOpportunityTarget = null;
        }
    }

    protected virtual bool IsValidOpportunityTarget(Collider2D other)
    {
        if (other == null) return false;
        return ((1 << other.gameObject.layer) & targetLayer) != 0;
    }

    private void CleanDetectedUnits()
    {
        for (int i = detectedUnits.Count - 1; i >= 0; i--)
        {
            if (detectedUnits[i] == null) detectedUnits.RemoveAt(i);
        }
    }

    private void PickOpportunityTarget()
    {
        Transform best = null;
        float bestDistSqr = float.MaxValue;
        Vector2 pos = transform.position;

        for (int i = 0; i < detectedUnits.Count; i++)
        {
            Transform t = detectedUnits[i];
            if (t == null) continue;
            float distSqr = ((Vector2)t.position - pos).sqrMagnitude;
            if (distSqr < bestDistSqr) { bestDistSqr = distSqr; best = t; }
        }

        currentOpportunityTarget = best;
    }

    // ----- State ticks -----

    private void TickStrategic()
    {
        // Refrescamos el top threat cada tick porque puede cambiar dinamicamente.
        currentStrategicTarget = ThreatRegistry.GetTopThreat();

        if (currentStrategicTarget == null)
        {
            state = AIState.Idle;
            if (agent != null && agent.hasPath) agent.ResetPath();
            return;
        }

        state = AIState.SeekStrategic;
        Vector3 targetPos = currentStrategicTarget.Transform.position;
        float dist = Vector2.Distance(transform.position, targetPos);

        if (agent != null) agent.stoppingDistance = stopDistanceStrategic;

        if (dist <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                attackDirectionVector = targetPos - transform.position;
                PerformAttack();
            }
        }
        else
        {
            RepathTo(targetPos);
        }
    }

    private void TickOpportunity()
    {
        state = AIState.ChaseOpportunity;
        Vector3 targetPos = currentOpportunityTarget.position;
        float dist = Vector2.Distance(transform.position, targetPos);

        if (agent != null) agent.stoppingDistance = stopDistanceOpportunity;

        if (dist <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                attackDirectionVector = targetPos - transform.position;
                PerformAttack();
            }
        }
        else
        {
            RepathTo(targetPos);
        }
    }

    protected void RepathTo(Vector3 destination)
    {
        if (agent == null || !agent.enabled) return;
        if (Time.time - lastRepathTime <= repathInterval) return;
        agent.SetDestination(destination);
        lastRepathTime = Time.time;
    }

    // ----- Attack -----

    protected virtual void PerformAttack()
    {
        isAttacking = true;
        canMove = false;
        if (agent != null && agent.hasPath) agent.ResetPath();

        int dirIndex = GetAttackDirectionIndex(attackDirectionVector);
        FaceAttackDirection(attackDirectionVector);
        ApplyAttackAnimatorParams(dirIndex);

        state = AIState.Attack;
        lastAttackTime = Time.time;

        Invoke(nameof(FinishAttack), attackAnimationDuration);
    }

    private void FinishAttack()
    {
        isAttacking = false;
        canMove = true;
    }

    protected abstract void ApplyAttackAnimatorParams(int directionIndex);

    /// <summary>
    /// Llamado desde un Animation Event en el clip de ataque. Busca objetivos
    /// en un peque�o c�rculo y les aplica da�o seg�n su tipo.
    /// </summary>
    public virtual void DetectAndDamageTargets()
    {
        Vector2 attackPoint = (Vector2)transform.position + attackDirectionVector.normalized * attackRange * 0.5f;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayer);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        List<Collider2D> hits = new List<Collider2D>();
        Physics2D.OverlapCircle(attackPoint, attackRange, filter, hits);

        foreach (Collider2D target in hits)
        {
            if (target == null) continue;
            Vector2 hitDirection = target.transform.position - transform.position;
            ApplyDamageToTarget(target, hitDirection);
        }
    }

    /// <summary>
    /// Aplica da�o a un objetivo concreto. Virtual por si una subclase quiere
    /// comportarse distinto (p. ej. un boss que hace m�s da�o a edificios).
    /// </summary>
    protected virtual void ApplyDamageToTarget(Collider2D target, Vector2 hitDirection)
    {
        IDamageReceiver receiver = target.GetComponentInParent<IDamageReceiver>();
        if (receiver == null) return;

        int damage = receiver.IsStructure ? GetDamageVsBuildings() : GetDamageVsUnits();
        bool applyForce = !receiver.IsStructure;
        receiver.ApplyDamage(damage, applyForce, false, hitDirection);
    }

    protected virtual int GetDamageVsUnits() => 1;
    protected virtual int GetDamageVsBuildings() => 5;

    // ----- Presentation -----

    private void UpdateAnimationAndFacing()
    {
        if (animator == null || agent == null) return;

        bool moving = !agent.isStopped && agent.hasPath && agent.desiredVelocity.sqrMagnitude > 0.01f;
        animator.SetBool("isRunning", moving);

        if (agent.desiredVelocity.x > 0.01f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (agent.desiredVelocity.x < -0.01f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    private void FaceAttackDirection(Vector2 dir)
    {
        if (dir.x > 0) transform.localScale = new Vector3(1f, 1f, 1f);
        else if (dir.x < 0) transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    protected int GetAttackDirectionIndex(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? 0 : 1;
        else
            return dir.y > 0 ? 2 : 3;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
}
