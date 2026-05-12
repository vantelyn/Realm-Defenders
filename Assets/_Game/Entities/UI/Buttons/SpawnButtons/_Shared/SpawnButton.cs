using UnityEngine;
using UnityEngine.UI;
using Game.Buildings;
using Game.Managers;

namespace Game.UI
{

/// <summary>
/// Boton de UI que spawnea una unidad. Refresca su estado cada frame (igual
/// patron que CastleBuildButton): barato y robusto frente a singletons que
/// aparecen tarde en la carga.
///   - requiredBuilding sin cumplir: semi-desactivado, hover muestra 'Requires X'.
///   - Requisito cumplido y recursos OK: activo.
/// </summary>
[RequireComponent(typeof(Button))]
public class SpawnButton : MonoBehaviour
{
    [SerializeField] private UnitSpawner spawner;
    [SerializeField] private UnitRecipe recipe;
    [Range(0.1f, 1f)] [SerializeField] private float disabledAlpha = 0.45f;

    public UnitRecipe Recipe => recipe;

    private Button button;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClicked);
    }

    private void Update()
    {
        Refresh();
    }

    private void OnClicked()
    {
        if (recipe == null) return;
        if (recipe.requiredBuilding != null && !BuildingRegistry.IsBuilt(recipe.requiredBuilding)) return;
        UnitSpawner active = ResolveSpawner();
        if (active == null) return;
        active.TrySpawn(recipe);
    }

    /// <summary>Devuelve el UnitSpawner asignado en inspector si existe, o el del primer
    /// Building vivo del requiredBuilding. Permite que el boton funcione sin referencia
    /// serializada (los buildings se crean en runtime).</summary>
    private UnitSpawner ResolveSpawner()
    {
        // Si hay requiredBuilding, el spawner SIEMPRE viene del building vivo (ignora
        // el campo serializado, que normalmente apunta a un spawner antiguo de escena).
        if (recipe != null && recipe.requiredBuilding != null)
        {
            Building b = BuildingRegistry.GetFirst(recipe.requiredBuilding);
            return b != null ? b.GetComponent<UnitSpawner>() : null;
        }
        return spawner;
    }

    private void Refresh()
    {
        if (button == null || recipe == null) return;
        bool requirementOk = recipe.requiredBuilding == null || BuildingRegistry.IsBuilt(recipe.requiredBuilding);
        bool canAfford = InventoryManager.Instance != null && recipe.CanAfford(InventoryManager.Instance);
        button.interactable = requirementOk && canAfford;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = requirementOk ? 1f : disabledAlpha;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
}
