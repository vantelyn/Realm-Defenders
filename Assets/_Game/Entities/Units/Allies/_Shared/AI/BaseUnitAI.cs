using UnityEngine;
using UnityEngine.AI;
using Game.Targeting;
using Game.Units;

namespace Game.AI
{

public abstract class BaseUnitAI : MonoBehaviour, IUnitAI
{
    [Header("References")]
    [SerializeField] protected TargetDetector enemyDetector;
    [SerializeField] protected TargetDetector resourceDetector;
    [Tooltip("Distancia minima al recurso para considerar 'llegado' (recogida la hace el ResourceCollector del PickupZone).")]
    [SerializeField] protected float resourceArriveDistance = 0.3f;

    [Header("Home Zone")]
    [SerializeField] protected float homeRadius = 1.5f;
    [SerializeField] protected float maxTravelDistance = 10f;

    [Header("Patrol")]
    [SerializeField] protected float patrolStepRadius = 1.0f;
    [SerializeField] protected float patrolPauseMin = 2.5f;
    [SerializeField] protected float patrolPauseMax = 5.0f;
    [SerializeField] protected float patrolArriveDistance = 0.15f;

    [Header("Repath")]
    [SerializeField] protected float repathInterval = 0.25f;

    protected PlayerUnit unit;
    protected NavMeshAgent agent;
    protected Rigidbody2D rb;

    protected Vector3 homePosition;
    protected float lastRepathTime = -999f;

    private float nextPatrolTime = -1f;
    protected bool hasPatrolDestination;

    protected virtual void Awake()
    {
        unit = GetComponent<PlayerUnit>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        homePosition = transform.position;
    }

    protected virtual void Start()
    {
        Enable();
    }

    public void SetHome(Vector3 position) { homePosition = position; }

    public virtual void Enable()
    {
        unit.SetMode(PlayerUnit.ControlMode.AI);

        if (!unit.IsGarrisoned)
        {
            if (agent != null)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                }
                agent.enabled = true;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        ResetStateOnEnable();
    }

    public virtual void Disable()
    {
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        if (rb != null && !unit.IsGarrisoned)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        ClearTargetsOnDisable();
        unit.SetMode(PlayerUnit.ControlMode.Player);
    }

    protected virtual void Update()
    {
        if (unit.Mode != PlayerUnit.ControlMode.AI) return;

        if (enemyDetector != null) enemyDetector.CleanNulls();
        if (resourceDetector != null) resourceDetector.CleanNulls();
        CleanDetectors();

        TickStates();

        if (agent != null && agent.enabled)
        {
            Vector2 vel = agent.velocity;
            unit.SetMovementInput(vel.sqrMagnitude > 0.01f ? vel.normalized : Vector2.zero);
        }
    }

    // ---- M�todos que las subclases implementan / sobrescriben ----

    /// <summary>Resetear estado al entrar en modo AI (normalmente ir a Idle).</summary>
    protected abstract void ResetStateOnEnable();

    /// <summary>Limpiar targets cacheados al salir de modo AI.</summary>
    protected abstract void ClearTargetsOnDisable();

    /// <summary>Dispatch al estado actual. Cada AI implementa su switch.</summary>
    protected abstract void TickStates();

    /// <summary>Limpieza adicional de detectores propios de la subclase.</summary>
    protected virtual void CleanDetectors() { }

    // ---- Helpers comunes de navegaci�n y targeting ----

    protected void TickPatrol()
    {
        if (hasPatrolDestination)
        {
            if (agent != null && agent.enabled && !agent.pathPending &&
                agent.remainingDistance <= patrolArriveDistance)
            {
                hasPatrolDestination = false;
                ScheduleNextPatrol();
                if (agent.hasPath) agent.ResetPath();
            }
            return;
        }

        if (nextPatrolTime < 0f) ScheduleNextPatrol();

        if (Time.time >= nextPatrolTime)
        {
            if (TryPickPatrolPoint(out Vector3 dest))
            {
                if (agent != null && agent.enabled)
                {
                    agent.SetDestination(dest);
                    hasPatrolDestination = true;
                }
            }
            else
            {
                nextPatrolTime = Time.time + 1f;
            }
        }
    }



    protected void ScheduleNextPatrol()
    {
        nextPatrolTime = Time.time + Random.Range(patrolPauseMin, patrolPauseMax);
    }

    protected bool TryPickPatrolPoint(out Vector3 dest)
    {
        Vector2 offset = Random.insideUnitCircle * patrolStepRadius;
        Vector3 candidate = homePosition + new Vector3(offset.x, offset.y, 0f);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolStepRadius, NavMesh.AllAreas))
        {
            dest = hit.position;
            return true;
        }
        dest = Vector3.zero;
        return false;
    }

    /// <summary>Pedir al agent que se dirija a un destino, respetando el intervalo de repath.</summary>
    protected void RepathTo(Vector3 destination)
    {
        if (agent == null || !agent.enabled) return;
        if (Time.time - lastRepathTime <= repathInterval) return;
        agent.SetDestination(destination);
        lastRepathTime = Time.time;
    }

    protected void StopAgent()
    {
        if (agent != null && agent.enabled)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
        }
    }

    protected void ResumeAgent()
    {
        if (agent != null && agent.enabled && agent.isStopped) agent.isStopped = false;
    }

    protected Transform FindClosestEnemy()
    {
        if (enemyDetector == null) return null;
        return enemyDetector.FindClosest(transform.position);
    }

    protected Transform FindClosestResource()
    {
        if (resourceDetector == null) return null;
        return resourceDetector.FindClosest(transform.position);
    }

    /// <summary>Radio aproximado del objetivo para calcular distancias entre bordes (no entre centros).
    /// Prioriza NavMeshAgent.radius (los enemigos lo tienen); fallback a la dimension menor del bounds del Collider2D.</summary>
    protected static float GetTargetRadius(Transform target)
    {
        if (target == null) return 0f;
        var ag = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (ag != null) return ag.radius;
        var col = target.GetComponent<Collider2D>();
        if (col != null)
        {
            Vector2 ext = col.bounds.extents;
            return Mathf.Min(ext.x, ext.y);
        }
        return 0.3f;
    }

    protected float DistanceToHome() => Vector2.Distance(transform.position, homePosition);

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, homeRadius);
    }

    public virtual void OnHostEnteredBuilding()
    {
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
    }

    public virtual void OnHostExitedBuilding()
    {
        if (rb != null)
        {
            rb.bodyType = unit.Mode == PlayerUnit.ControlMode.Player
                ? RigidbodyType2D.Dynamic
                : RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        if (unit.Mode == PlayerUnit.ControlMode.AI && agent != null)
        {
            agent.enabled = true;
        }
    }
}
}
