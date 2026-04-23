using UnityEngine;

public abstract class PlayerUnit : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    [Header("Combat")]
    public int maxHealth = 500;
    public float attackRange = 1.2f;
    public LayerMask targetLayer;

    [Header("Selection")]
    [SerializeField] protected GameObject selectionIndicator;

    protected Rigidbody2D rb2D;
    protected Animator animator;
    protected Vector2 movementInput;
    protected Vector2 lastMovementDir = Vector2.right;
    protected Vector2 attackDir;
    protected bool isAttacking;

    public bool canMove = true;
    public bool IsSelected { get; private set; }
    public virtual bool HasSecondary => false;
    public bool IsAttacking => isAttacking;

    protected virtual void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (selectionIndicator != null) selectionIndicator.SetActive(false);
    }

    protected virtual void Update()
    {
        movementInput = IsSelected ? CameraFollowController.ReadWasd() : Vector2.zero;

        bool moving = movementInput.sqrMagnitude > 0.01f;
        animator.SetBool("isRunning", moving);
        if (moving) lastMovementDir = movementInput;
        CheckFlip();
    }

    protected virtual void FixedUpdate()
    {
        if (canMove)
            rb2D.linearVelocity = movementInput * speed;
    }

    public virtual void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (selectionIndicator != null) selectionIndicator.SetActive(selected);

        if (!selected)
        {
            movementInput = Vector2.zero;
            if (canMove) rb2D.linearVelocity = Vector2.zero;
        }
    }

    public abstract void PrimaryAttack(Vector2 worldAimDirection);
    public virtual void SecondaryAction(Vector2 worldAimDirection) { }

    // Animation Events
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

    protected void CheckFlip()
    {
        if ((movementInput.x > 0 && transform.localScale.x < 0) ||
            (movementInput.x < 0 && transform.localScale.x > 0))
        {
            Vector3 s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }
    }

    protected int GetDirectionIndex(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? 0 : 1;
        else
            return dir.y > 0 ? 2 : 3;
    }
}