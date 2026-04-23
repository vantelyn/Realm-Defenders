using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Se añade al mismo GameObject que el BuildButton. Muestra el tooltip con la
/// información de la receta cuando el ratón entra en el botón.
/// </summary>
public class BuildTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private BuildTooltip tooltip;
    [SerializeField] private BuildingRecipe recipe;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null && recipe != null) tooltip.Show(recipe);
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