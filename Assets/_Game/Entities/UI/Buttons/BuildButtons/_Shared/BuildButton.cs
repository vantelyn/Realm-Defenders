using UnityEngine;
using UnityEngine.UI;
using Game.Buildings;
using Game.Managers;

namespace Game.UI
{

/// <summary>
/// Boton de UI que entra en modo construccion. Refresca cada frame (mismo
/// patron que CastleBuildButton).
///   - isUnique + ya construido: semi-desactivado, hover muestra 'Already built'.
///   - Construible y recursos OK: activo.
/// </summary>
[RequireComponent(typeof(Button))]
public class BuildButton : MonoBehaviour
{
    [SerializeField] private BuildingManager placer;
    [SerializeField] private BuildingRecipe recipe;
    [Range(0.1f, 1f)] [SerializeField] private float disabledAlpha = 0.45f;

    public BuildingRecipe Recipe => recipe;

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
        if (placer == null || recipe == null) return;
        if (recipe.isUnique && BuildingRegistry.IsBuilt(recipe)) return;
        placer.BeginPlacement(recipe);
    }

    private void Refresh()
    {
        if (button == null || recipe == null) return;
        bool alreadyBuilt = recipe.isUnique && BuildingRegistry.IsBuilt(recipe);
        bool canAfford = InventoryManager.Instance != null && recipe.CanAfford(InventoryManager.Instance);
        button.interactable = !alreadyBuilt && canAfford;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alreadyBuilt ? disabledAlpha : 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
}
