using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Se añade al mismo GameObject que un BuildButton o SpawnButton. Muestra el
/// tooltip con la información de la receta cuando el ratón entra en el botón.
/// </summary>
public class BuildTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private BuildTooltip tooltip;
    [Tooltip("Arrastra aquí un BuildingRecipe o UnitRecipe.")]
    [SerializeField] private ScriptableObject recipeAsset;

    private IRecipe Recipe => recipeAsset as IRecipe;

    public void OnPointerEnter(PointerEventData eventData)
    {
        IRecipe r = Recipe;
        if (tooltip != null && r != null) tooltip.Show(r);
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