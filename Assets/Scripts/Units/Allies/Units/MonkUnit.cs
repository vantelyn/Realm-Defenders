using UnityEngine;

public class MonkUnit : PlayerUnit
{
    [Header("Healing")]
    [SerializeField] private float healRange = 3f;
    [SerializeField] private int healAmount = 1;
    [SerializeField] private GameObject healEffectPrefab;

    private PlayerUnit pendingHealTarget;

    public override bool HasSecondary => true;
    public override bool SecondaryTargetsAllies => true;

    public override void PrimaryAttack(Vector2 worldAimDirection) { }

    public override void SecondaryAction(Vector2 worldAimDirection, PlayerUnit hoveredUnit)
    {
        if (isAttacking) return;
        if (hoveredUnit == null) return;
        if (hoveredUnit.GetComponent<DamageReceiverPlayer>() == null) return;

        float distance = Vector2.Distance(transform.position, hoveredUnit.transform.position);
        if (distance > healRange) return;

        pendingHealTarget = hoveredUnit;
        animator.SetTrigger("doHeal");
    }

    // Animation Event en el clip doHeal
    public void ApplyHeal()
    {
        if (pendingHealTarget == null) return;

        DamageReceiverPlayer receiver = pendingHealTarget.GetComponent<DamageReceiverPlayer>();
        if (receiver != null) receiver.Heal(healAmount);

        if (healEffectPrefab != null)
        {
            Instantiate(healEffectPrefab, pendingHealTarget.transform.position, Quaternion.identity, pendingHealTarget.transform);
        }

        pendingHealTarget = null;
    }
}