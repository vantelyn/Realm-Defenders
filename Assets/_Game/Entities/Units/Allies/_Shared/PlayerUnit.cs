using System.Collections.Generic;
using UnityEngine;
using Game.AI;
using Game.Buildings;
using Game.Managers;
using Game.Combat;

namespace Game.Units
{

public abstract class PlayerUnit : MonoBehaviour
{
    public enum ControlMode { Player, AI }

    [Header("Movement")]
    public float speed = 5f;

    [Header("Combat")]
    public int maxHealth = 500;
    public float attackRange = 1.2f;
    public LayerMask targetLayer;
    public DamageRule[] damageRules;

    [Header("Selection")]
    [SerializeField] protected GameObject selectionIndicator;

    protected Rigidbody2D rb2D;
    protected Animator animator;
    protected Vector2 movementInput;
    protected Vector2 attackDir;
    protected bool isAttacking;

    private IUnitAI ai;
    private Building currentBuilding;
    private SpriteRenderer cachedSpriteRenderer;
    private DamageReceiverPlayer cachedHealth;
    private int originalSortingOrder;
    private Collider2D rootCollider;
    private bool rootColliderWasTrigger;

    public bool canMove = true;
    public bool IsSelected { get; private set; }
    public virtual bool HasSecondary => false;
    public virtual bool SecondaryTargetsAllies => false;
    public bool IsAttacking => isAttacking;
    /// <summary>True si la unit esta en un estado que requiere bloquear movimiento (ataque, bloqueo, etc.). Override en subclases para anadir nuevos estados.</summary>
    public virtual bool IsBusy => isAttacking;
    public bool IsGarrisoned => currentBuilding != null;
    public Building CurrentBuilding => currentBuilding;
    public float RangeMultiplier => currentBuilding != null ? currentBuilding.RangeMultiplier : 1f;
    public DamageReceiverPlayer Health => cachedHealth;

    public Vector2 LastMovementDir { get; set; } = Vector2.right;
    public ControlMode Mode { get; private set; } = ControlMode.Player;

    protected virtual void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        ai = GetComponent<IUnitAI>();
        cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cachedHealth = GetComponent<DamageReceiverPlayer>();
        rootCollider = GetComponent<Collider2D>();
        if (selectionIndicator != null) selectionIndicator.SetActive(false);
    }

    protected virtual void Update()
    {
        if (Mode == ControlMode.Player && IsSelected && !isAttacking && !IsGarrisoned && canMove)
        {
            movementInput = CameraManager.ReadWasd();
        }
        else if (Mode == ControlMode.Player)
        {
            movementInput = Vector2.zero;
        }

        bool moving = movementInput.sqrMagnitude > 0.01f;
        animator.SetBool("isRunning", moving);
        if (moving) LastMovementDir = movementInput;

        CheckFlip();
    }

    protected virtual void FixedUpdate()
    {
        if (canMove && Mode == ControlMode.Player && !IsGarrisoned)
        {
            rb2D.linearVelocity = movementInput * speed;
        }
    }

    public void SetMode(ControlMode mode)
    {
        if (Mode == mode) return;
        Mode = mode;
        if (mode == ControlMode.AI)
        {
            movementInput = Vector2.zero;
        }
    }

    public void SetMovementInput(Vector2 input)
    {
        movementInput = input;
    }

    public virtual void SetSelected(bool selected)
    {
        if (IsSelected == selected) return;
        IsSelected = selected;
        if (selectionIndicator != null) selectionIndicator.SetActive(selected);

        if (!selected)
        {
            movementInput = Vector2.zero;
            if (canMove && Mode == ControlMode.Player && !IsGarrisoned) rb2D.linearVelocity = Vector2.zero;
            OnBecameUnselected();
        }
        else
        {
            OnBecameSelected();
        }
    }

    protected virtual void OnBecameSelected()
    {
        if (ai != null) ai.Disable();
    }

    protected virtual void OnBecameUnselected()
    {
        if (ai != null)
        {
            ai.SetHome(transform.position);
            ai.Enable();
        }
    }

    public virtual void OnEnteredBuilding(Building building, Transform slot)
    {
        currentBuilding = building;
        movementInput = Vector2.zero;
        if (rb2D != null) rb2D.linearVelocity = Vector2.zero;

        if (ai != null) ai.OnHostEnteredBuilding();

        transform.position = slot.position;
        canMove = false;
        if (animator != null) animator.SetBool("isRunning", false);

        if (cachedSpriteRenderer != null)
        {
            originalSortingOrder = cachedSpriteRenderer.sortingOrder;
            cachedSpriteRenderer.sortingOrder = 10;
        }

        // Garrison: el hitbox se mantiene activo (selecion + recepcion de dano).
        // El root collider pasa a trigger para no bloquear fisicamente a otras unidades
        // que pasen cerca del building. La inmovilidad la dan canMove=false + Kinematic + AI disabled.
        // Sin knockback en garrison: bloqueado dentro de DamageReceiverPlayer.ApplyDamage.
        if (rootCollider != null) { rootColliderWasTrigger = rootCollider.isTrigger; rootCollider.isTrigger = true; }
    }

    public virtual void OnExitedBuilding(Vector3 doorPosition)
    {
        currentBuilding = null;
        transform.position = doorPosition;
        canMove = true;

        if (ai != null) ai.OnHostExitedBuilding();
        else if (rb2D != null)
        {
            // Fallback para unidades sin AI.
            rb2D.bodyType = RigidbodyType2D.Dynamic;
            rb2D.linearVelocity = Vector2.zero;
        }

        if (cachedSpriteRenderer != null) cachedSpriteRenderer.sortingOrder = originalSortingOrder;

        // Restaurar isTrigger del root al salir
        if (rootCollider != null) rootCollider.isTrigger = rootColliderWasTrigger;
    }

    public abstract void PrimaryAttack(Vector2 worldAimDirection);
    public virtual void SecondaryAction(Vector2 worldAimDirection, PlayerUnit hoveredUnit) { }
    public virtual void EndSecondaryAction() { }

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

    public virtual void DetectAndDamageTargets()
    {
        Vector2 attackPoint = (Vector2)transform.position + attackDir.normalized * attackRange * 0.5f;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayer);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        List<Collider2D> hits = new List<Collider2D>();
        Physics2D.OverlapCircle(attackPoint, attackRange, filter, hits);

        foreach (Collider2D target in hits)
        {
            if (target == null) continue;

            DamageRule rule = GetRuleForLayer(target.gameObject.layer);
            if (rule == null) continue;

            IDamageReceiver receiver = target.GetComponentInParent<IDamageReceiver>();
            if (receiver == null) continue;

            GameObject targetRoot = ((Component)receiver).gameObject;
            float ratio = receiver.IsStructure ? 1f : CombatHelper.GetMassRatio(gameObject, targetRoot);
            int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(rule.damage * ratio));

            Vector2 hitDirection = target.transform.position - transform.position;
            receiver.ApplyDamage(scaledDamage, rule.applyForce, rule.applyHitAnimation, hitDirection, ratio);
        }
    }

    private DamageRule GetRuleForLayer(int layer)
    {
        if (damageRules == null) return null;
        for (int i = 0; i < damageRules.Length; i++)
        {
            DamageRule r = damageRules[i];
            if (r == null || string.IsNullOrEmpty(r.layerName)) continue;
            if (LayerMask.NameToLayer(r.layerName) == layer) return r;
        }
        return null;
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
}
