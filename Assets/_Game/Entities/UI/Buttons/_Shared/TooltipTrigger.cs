using UnityEngine;
using UnityEngine.EventSystems;
using Game.Buildings;

namespace Game.UI
{

/// <summary>
/// Se anade al mismo GameObject que un BuildButton o SpawnButton. Muestra el
/// tooltip con la informacion de la receta cuando el raton entra. Si detecta
/// un SpawnButton sibling con requisito sin cumplir, muestra el modo 
/// ShowMissingRequirement. Si detecta un BuildButton con instancia ya construida
/// y isUnique, muestra ShowAlreadyBuilt.
/// </summary>
public class BuildTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private BuildTooltip tooltip;
    [Tooltip("Arrastra aqui un BuildingRecipe o UnitRecipe. Si esta vacio, se intenta tomar del sibling SpawnButton/BuildButton.")]
    [SerializeField] private ScriptableObject recipeAsset;

    private IRecipe Recipe => recipeAsset as IRecipe;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip == null) return;

        // Caso A: SpawnButton sibling con UnitRecipe (puede tener requisito)
        var spawnBtn = GetComponent<SpawnButton>();
        if (spawnBtn != null)
        {
            UnitRecipe ur = spawnBtn.Recipe;
            if (ur == null) return;
            if (ur.requiredBuilding != null && !BuildingRegistry.IsBuilt(ur.requiredBuilding))
            {
                tooltip.ShowMissingRequirement(ur, ur.requiredBuilding);
                return;
            }
            tooltip.Show(ur);
            return;
        }

        // Caso B: BuildButton sibling con BuildingRecipe (puede estar ya construido)
        var buildBtn = GetComponent<BuildButton>();
        if (buildBtn != null)
        {
            BuildingRecipe br = buildBtn.Recipe;
            if (br == null) return;
            if (br.isUnique && BuildingRegistry.IsBuilt(br))
            {
                tooltip.ShowAlreadyBuilt(br);
                return;
            }
            tooltip.Show(br);
            return;
        }

        // Caso C: legacy fallback al campo recipeAsset
        IRecipe r = Recipe;
        if (r != null) tooltip.Show(r);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null) tooltip.Hide();
    }

    private void OnDisable()
    {
        if (tooltip != null) tooltip.Hide();
    }
}
}
