using UnityEngine;
using Game.Combat;

namespace Game.Units
{

public class MonkUnit : PlayerUnit
{
    [Header("SFX")]
    [SerializeField] private AudioClip healClip;
    [Range(0f,1f)] [SerializeField] private float healVolume = 0.9f;
    [SerializeField] private AudioSource sfxSource;
    private void EnsureSfx() {
        if (sfxSource != null) return;
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false; sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = 4f; sfxSource.maxDistance = 22f; sfxSource.dopplerLevel = 0f;
    }
    [Header("Healing")]
    [SerializeField] private float healRange = 3f;
    [SerializeField] private int healAmount = 1;
    [SerializeField] private GameObject healEffectPrefab;

    private PlayerUnit pendingHealTarget;

    public override bool HasSecondary => true;
    public override bool SecondaryTargetsAllies => true;
    public float HealRange => healRange * RangeMultiplier;

    // El monje no tiene ataque primario.
    public override void PrimaryAttack(Vector2 worldAimDirection) { }

    // Ruta del jugador: clic derecho. SelectionManager nos pasa la unidad hovereada.
    public override void SecondaryAction(Vector2 worldAimDirection, PlayerUnit hoveredUnit)
    {
        if (Mode != ControlMode.Player) return;
        TryHeal(hoveredUnit);
    }

    // Ruta compartida: la IA llama a esto con un objetivo expl�cito.
    public bool TryHeal(PlayerUnit target)
    {
        if (isAttacking) return false;
        if (target == null) return false;

        DamageReceiverPlayer receiver = target.GetComponent<DamageReceiverPlayer>();
        if (receiver == null) return false;
        if (receiver.IsAtFullHealth) return false;

        float distance = Vector2.Distance(transform.position, target.transform.position);
        if (distance > HealRange) return false;

        pendingHealTarget = target;
        animator.SetTrigger("doHeal");
        return true;
    }

    // Animation Event
    public void ApplyHeal()
    {
        if (pendingHealTarget == null) return;
        DamageReceiverPlayer receiver = pendingHealTarget.GetComponent<DamageReceiverPlayer>();
        if (receiver != null && !receiver.IsAtFullHealth) { receiver.Heal(healAmount); EnsureSfx(); if (sfxSource != null && healClip != null) sfxSource.PlayOneShot(healClip, healVolume); }
        if (healEffectPrefab != null)
        {
            Instantiate(healEffectPrefab, pendingHealTarget.transform.position, Quaternion.identity, pendingHealTarget.transform);
        }
        pendingHealTarget = null;
    }
}
}
