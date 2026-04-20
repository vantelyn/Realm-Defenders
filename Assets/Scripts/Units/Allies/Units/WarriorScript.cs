using UnityEngine;

public class WarriorScript : MonoBehaviour
{
    public float speed = 5;
    Rigidbody2D rb2D;
    Vector2 movementInput;
    private Animator animator;
    public int maxHealth = 500;
    private bool isAttacking = false;
    public bool canMove = true;
    Vector2 lastMovementDir = Vector2.right;
    private int blockIndex = 2;
    private Vector2 attackDir;
    public float attackRange = 1.2f;
    public LayerMask targetLayer;


    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {


        if (movementInput != Vector2.zero)
        {
            lastMovementDir = movementInput;
        }

        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput = movementInput.normalized;

        if (Mathf.Abs(movementInput.x) > 0.1 || Mathf.Abs(movementInput.y) > 0.1)
        {
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }

        CheckFlip();
        Attack();
    }

    private void FixedUpdate()
    {
        if (canMove)
        {
            rb2D.linearVelocity = movementInput * speed;
        }
    }

    private void CheckFlip()
    {
        if (movementInput.x > 0 && transform.localScale.x < 0 || movementInput.x < 0 && transform.localScale.x > 0)
        {
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        }
    }

    private void Attack()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            int dir = GetDirectionIndex(lastMovementDir);
            attackDir = GetAttackInputDirection();
            int attackDirection = GetDirectionIndex(attackDir);
            animator.SetInteger("attackDirection", attackDirection);
            int attackIndex = Random.Range(0, 2);
            animator.SetInteger("attackIndex", attackIndex);
            animator.SetTrigger("doAttack");
        }
        if (Input.GetMouseButtonDown(1) && !isAttacking)
        {
            animator.SetInteger("attackIndex", blockIndex);
            animator.SetTrigger("doAttack");
        }
    }

    public void StartAttack()
    {
        isAttacking = true;
        rb2D.linearVelocity = Vector2.zero;
        canMove = false;
    }

    public void EndAttack()
    {
        isAttacking = false;
        canMove = true;
    }

    Vector2 GetAttackInputDirection()
    {
        Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (inputDir != Vector2.zero) 
        {
            return inputDir;
        }
        else
        {
            if (transform.localScale.x > 0)
                return Vector2.right;
            else
                return Vector2.left;
        }
    }

    int GetDirectionIndex(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? 0 : 1;
        else
            return dir.y > 0 ? 2 : 3;
    }

    public void DetectAndDamageTargets()
    {
        Vector2 attackPoint = (Vector2)transform.position + attackDir.normalized * attackRange * 0.5f;
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint, attackRange, targetLayer);

        foreach (Collider2D target in hitTargets)
        {
            Vector2 hitDirection = target.transform.position - transform.position;
            GameObject obj = target.gameObject;
            int layer = obj.layer;

            if (layer == LayerMask.NameToLayer("Enemy"))
            {
                obj.GetComponent<DamageReceiver>().ApplyDamage(1, true, false, hitDirection);
            }
            else if (layer == LayerMask.NameToLayer("Sheep"))
            {
                obj.GetComponent<DamageReceiver>().ApplyDamage(1, true, false, hitDirection);
            }
            else if (layer == LayerMask.NameToLayer("Tree"))
            {
                obj.GetComponent<DamageReceiver>().ApplyDamage(1, false, true, hitDirection);
            }

        }
    }

}
