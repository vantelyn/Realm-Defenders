using UnityEngine;
using UnityEngine.AI;

public class Enemy : NPC
{
    public float attackRange = 1.5f;
    public float stopDistance = 0.5f;
    public float attackCooldown = 2f;
    private float lastAttackTime = 0f;

    private bool isAttacking = false;
    private bool canMove = true;

    public LayerMask targetLayer;
    private Vector2 playerDirection;

    private Transform motherTransform;

    protected override void Start()
    {
        base.Start();
        FindMotherByTagPlayerAndLayerMother();
    }

    protected override void Update()
    {
        base.Update();

        if (motherTransform == null)
        {
            FindMotherByTagPlayerAndLayerMother();
            return;
        }

        float distanceToMother = Vector2.Distance(transform.position, motherTransform.position);

        if (!isAttacking)
        {
            navMeshAgent.stoppingDistance = stopDistance;
            navMeshAgent.SetDestination(motherTransform.position);
        }

        if (distanceToMother <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            playerDirection = motherTransform.position - transform.position;
            Attack();
            lastAttackTime = Time.time;
        }
    }

    private void FindMotherByTagPlayerAndLayerMother()
    {
        GameObject[] taggedAsPlayer = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject obj in taggedAsPlayer)
        {
            if (obj.layer == LayerMask.NameToLayer("Mother"))
            {
                motherTransform = obj.transform;
                return;
            }
        }

        motherTransform = null;
    }

    private void Attack()
    {
        isAttacking = true;
        canMove = false;
        navMeshAgent.ResetPath();

        int attackDirection = GetAttackDirection(playerDirection);

        if (playerDirection.x > 0)
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
        Vector2 attackPoint = (Vector2)transform.position + playerDirection.normalized * attackRange * 0.5f;
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint, attackRange, targetLayer);

        foreach (Collider2D target in hitTargets)
        {
            Vector2 hitDirection = target.transform.position - transform.position;
            GameObject obj = target.gameObject;
            int layer = obj.layer;

            if (layer == LayerMask.NameToLayer("Mother"))
            {
                obj.GetComponent<MotherHealth>()?.TakeDamage(5f);
            }

            if (layer == LayerMask.NameToLayer("Player"))
            {
                obj.GetComponent<DamageReceiver>()?.ApplyDamage(1, true, false, hitDirection);
            }
        }
    }
}