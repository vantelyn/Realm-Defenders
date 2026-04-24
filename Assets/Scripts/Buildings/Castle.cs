using UnityEngine;

/// <summary>
/// Único castillo por partida. Gestiona sus 3 niveles:
/// - Lv1 → Lv2: upgrade por recursos.
/// - Lv2 → Lv3: auto-evolución por tiempo.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(DamageReceiverBuilding))]
[RequireComponent(typeof(StrategicTarget))]
public class Castle : MonoBehaviour
{
    public static Castle Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private CastleUpgradeData data;

    [Header("Initial Level")]
    [SerializeField] private int startLevel = 1;

    private int currentLevel;
    private float evolutionTimer;
    private bool evolving;

    private Animator animator;
    private DamageReceiverBuilding health;
    private StrategicTarget strategicTarget;

    public int CurrentLevel => currentLevel;
    public bool IsMaxLevel => currentLevel >= (data != null ? data.MaxLevel : 1);
    public bool IsEvolving => evolving;
    public float EvolutionProgress => evolving && data != null ? Mathf.Clamp01(evolutionTimer / data.evolutionDuration) : 0f;
    public CastleUpgradeData Data => data;

    public event System.Action<int> OnLevelChanged;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<DamageReceiverBuilding>();
        strategicTarget = GetComponent<StrategicTarget>();
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[Castle] Ya existe otra instancia ({Instance.name}). Destruyendo {name}.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ApplyLevel(startLevel, isInitial: true);
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!evolving) return;

        evolutionTimer += Time.deltaTime;
        if (evolutionTimer >= data.evolutionDuration)
        {
            evolving = false;
            evolutionTimer = 0f;
            ApplyLevel(currentLevel + 1);
        }
    }

    /// <summary>
    /// Intenta hacer upgrade pagando del inventario. Solo funciona si:
    /// - No está evolucionando ya.
    /// - Hay un nivel superior disponible que requiere recursos (Lv1→Lv2).
    /// - El jugador puede pagar.
    /// </summary>
    public bool TryUpgrade(PlayerInventory inventory)
    {
        if (data == null || inventory == null) return false;
        if (IsMaxLevel || evolving) return false;

        int nextLevel = currentLevel + 1;
        if (!data.HasLevel(nextLevel)) return false;

        CastleUpgradeData.Level next = data.GetLevel(nextLevel);

        // Si el siguiente nivel no requiere recursos, se entra por auto-evolución, no por botón.
        bool hasCost = next.woodCost > 0 || next.meatCost > 0 || next.moneyCost > 0;
        if (!hasCost) return false;

        if (!CanAfford(inventory, next)) return false;

        Pay(inventory, next);
        ApplyLevel(nextLevel);

        // Si el siguiente nivel (Lv3) es por tiempo, arrancar el timer.
        int afterThis = nextLevel + 1;
        if (data.HasLevel(afterThis))
        {
            CastleUpgradeData.Level auto = data.GetLevel(afterThis);
            bool autoHasCost = auto.woodCost > 0 || auto.meatCost > 0 || auto.moneyCost > 0;
            if (!autoHasCost)
            {
                evolving = true;
                evolutionTimer = 0f;
            }
        }

        return true;
    }

    private void ApplyLevel(int newLevel, bool isInitial = false)
    {
        currentLevel = Mathf.Clamp(newLevel, 1, data != null ? data.MaxLevel : 1);

        if (data == null) return;

        CastleUpgradeData.Level lvl = data.GetLevel(currentLevel);

        // Stats.
        if (health != null)
        {
            health.maxHealth = lvl.maxHealth;
            if (!isInitial) health.Heal(lvl.maxHealth);  // cura al 100% al evolucionar.
            else health.ResetToFull(lvl.maxHealth);
        }

        if (strategicTarget != null) strategicTarget.SetThreatLevel(lvl.threatLevel);

        // Animación.
        if (animator != null) animator.SetInteger("stage", currentLevel);

        OnLevelChanged?.Invoke(currentLevel);
    }

    public bool CanAffordNextUpgrade(PlayerInventory inventory)
    {
        if (data == null || inventory == null || IsMaxLevel || evolving) return false;
        int nextLevel = currentLevel + 1;
        if (!data.HasLevel(nextLevel)) return false;

        CastleUpgradeData.Level next = data.GetLevel(nextLevel);
        bool hasCost = next.woodCost > 0 || next.meatCost > 0 || next.moneyCost > 0;
        if (!hasCost) return false;

        return CanAfford(inventory, next);
    }

    public CastleUpgradeData.Level GetNextUpgradeLevel()
    {
        if (data == null || IsMaxLevel) return null;
        int nextLevel = currentLevel + 1;
        if (!data.HasLevel(nextLevel)) return null;
        return data.GetLevel(nextLevel);
    }

    private static bool CanAfford(PlayerInventory inv, CastleUpgradeData.Level lvl)
    {
        return inv.Wood >= lvl.woodCost
            && inv.Meat >= lvl.meatCost
            && inv.Money >= lvl.moneyCost;
    }

    private static void Pay(PlayerInventory inv, CastleUpgradeData.Level lvl)
    {
        if (lvl.woodCost > 0) inv.TrySpendWood(lvl.woodCost);
        if (lvl.meatCost > 0) inv.TrySpendMeat(lvl.meatCost);
        if (lvl.moneyCost > 0) inv.TrySpendMoney(lvl.moneyCost);
    }
}