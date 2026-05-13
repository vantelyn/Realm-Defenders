using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
public class DemolishTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private BuildTooltip tooltip;
    [SerializeField] private string title = "Demolish";
    [SerializeField] [TextArea(2,5)] private string description = "Click to remove one building. Shift+click to keep demolish mode active until cancelled (Esc / right-click).";
    public void OnPointerEnter(PointerEventData e) { if (tooltip != null) tooltip.ShowText(title, description); }
    public void OnPointerExit(PointerEventData e) { if (tooltip != null) tooltip.Hide(); }
    private void OnDisable() { if (tooltip != null) tooltip.Hide(); }
}
}
