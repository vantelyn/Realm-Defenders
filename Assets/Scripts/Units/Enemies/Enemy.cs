using System.Collections.Generic;
using UnityEngine;

public class Enemy : NPC
{
    [Header("Attack Mother")]
    public float attackRange = 1.5f;
    public float stopDistance = 0.5f;
    public float attackCooldown = 2f;
    private float lastAttackTime = 0f;

    private bool isAttacking = false;
    private bool canMove = true;

    public LayerMask targetLayer;
    private Vector2 attackDirectionVector;

    private Transform motherTransform;

    [Header("Chase Player Unit")]
    public float chaseRange = 6f;
    public float stopDistanceFromPlayer = 1.5f;
    protected bool isChasingPlayerUnit = false;

    private readonly List<Transform> detectedUnits = new List<Transform>();
    private Transform currentTargetUnit;

    protected override void Start()
    {
        base.Start();
        FindMother();
    }

    protected override void Update()
    {
        base.Update();

        CleanDetectedUnits();
        UpdateCurrentTarget();

        if (isAttacking)
            return;

        if (currentTargetUnit != null)
        {
            HandleChaseUnit();
        }
        else
        {
            HandleMotherLogic();
        }
    }

    private void FindMother()
    {
        GameObject[] taggedAsMother = GameObject.FindGameObjectsWithTag("Mother");
        if (taggedAsMother.Length > 0)
        {
            motherTransform = taggedAsMother[0].transform;
        }
    }

    private void HandleMotherLogic()
    {
        isChasingPlayerUnit = false;

        if (motherTransform == null)
        {
            FindMother();
            return;
        }

        float distanceToMother = Vector2.Distance(transform.position, motherTransform.position);

        navMeshAgent.stoppingDistance = stopDistance;
        navMeshAgent.SetDestination(motherTransform.position);

        if (distanceToMother <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            attackDirectionVector = motherTransform.position - transform.position;
            Attack();
            lastAttackTime = Time.time;
        }
    }

    private void HandleChaseUnit()
    {
        if (currentTargetUnit == null)
            return;

        isChasingPlayerUnit = true;

        float distanceToUnit = Vector2.Distance(transform.position, currentTargetUnit.position);

        navMeshAgent.stoppingDistance = stopDistanceFromPlayer;
        navMeshAgent.SetDestination(currentTargetUnit.position);

        if (distanceToUnit <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            attackDirectionVector = currentTargetUnit.position - transform.position;
            Attack();
            lastAttackTime = Time.time;
        }
    }

    private void Attack()
    {
        isAttacking = true;
        canMove = false;
        navMeshAgent.ResetPath();

        int attackDirection = GetAttackDirection(attackDirectionVector);

        if (attackDirectionVector.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);

        animator.SetInteger("attackDirection", attackDirection);
        animator.SetTrigger("doAttack");

        Invoke(nameof(ResetAttack), 0.5f);
    }

    private void ResetAttack()
    {
        isAttacking = false;
        canMove = true;
    }

    private void FixedUpdate()
    {
        navMeshAgent.isStopped = !canMove;
    }

    private int GetAttackDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0 ? 0 : 1;
        else
            return direction.y > 0 ? 2 : 3;
    }

    public void DetectAndDamageTargets()
    {
        Vector2 attackPoint = (Vector2)transform.position + attackDirectionVector.normalized * attackRange * 0.5f;
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint, attackRange, targetLayer);

        foreach (Collider2D target in hitTargets)
        {
            if (target == null) continue;

            Vector2 hitDirection = target.transform.position - transform.position;
            GameObject obj = target.gameObject;
            string tag = obj.tag;

            if (tag == "Mother")
            {
                obj.GetComponent<MotherHealth>()?.TakeDamage(5f);
            }
            else if (tag == "PlayerUnit")
            {
                obj.GetComponent<DamageReceiverPlayer>()?.ApplyDamage(1, true, false, hitDirection);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidUnitTarget(other))
            return;

        Transform targetTransform = other.transform;

        if (!detectedUnits.Contains(targetTransform))
            detectedUnits.Add(targetTransform);

        UpdateCurrentTarget();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidUnitTarget(other))
            return;

        Transform targetTransform = other.transform;

        if (detectedUnits.Contains(targetTransform))
            detectedUnits.Remove(targetTransform);

        if (currentTargetUnit == targetTransform)
            currentTargetUnit = null;

        UpdateCurrentTarget();
    }

    private bool IsValidUnitTarget(Collider2D other)
    {
        if (other == null) return false;
        if (!other.CompareTag("PlayerUnit")) return false;

        return ((1 << other.gameObject.layer) & targetLayer) != 0;
    }

    private void CleanDetectedUnits()
    {
        for (int i = detectedUnits.Count - 1; i >= 0; i--)
        {
            if (detectedUnits[i] == null)
            {
                detectedUnits.RemoveAt(i);
            }
        }

        if (currentTargetUnit == null)
            isChasingPlayerUnit = false;
    }

    private void UpdateCurrentTarget()
    {
        Transform bestTarget = null;
        float bestDistance = Mathf.Infinity;

        for (int i = 0; i < detectedUnits.Count; i++)
        {
            Transform candidate = detectedUnits[i];
            if (candidate == null) continue;

            float distance = Vector2.Distance(transform.position, candidate.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = candidate;
            }
        }

        currentTargetUnit = bestTarget;
        isChasingPlayerUnit = currentTargetUnit != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}