using UnityEngine;
using UnityEngine.EventSystems;
using Game.UI;

namespace Game.Buildings
{

/// <summary>
/// Muestra en el tooltip el contenido adecuado seg�n si existe castillo:
/// - Sin castillo: la receta de construcci�n inicial.
/// - Con castillo: una receta sint�tica del siguiente upgrade.
/// </summary>
public class CastleTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private BuildTooltip tooltip;
    [SerializeField] private BuildingRecipe initialRecipe;

    private CastleLevelRecipe dynamicRecipe;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip == null) return;

        IRecipe target = GetCurrentRecipe();
        if (target != null) tooltip.Show(target);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null) tooltip.Hide();
    }

    private void OnDisable()
    {
        if (tooltip != null) tooltip.Hide();
    }

    private IRecipe GetCurrentRecipe()
    {
        if (Castle.Instance == null) return initialRecipe;

        Castle castle = Castle.Instance;
        if (castle.IsMaxLevel) return new CastleMaxRecipe();
        if (castle.IsEvolving) return new CastleEvolvingRecipe();

        CastleUpgradeData.Level next = castle.GetNextUpgradeLevel();
        if (next == null) return initialRecipe;

        if (dynamicRecipe == null) dynamicRecipe = new CastleLevelRecipe();
        dynamicRecipe.Setup(next);
        return dynamicRecipe;
    }
}

/// <summary>Receta sint�tica: refleja el siguiente upgrade del castillo.</summary>
public class CastleLevelRecipe : IRecipe
{
    private CastleUpgradeData.Level level;
    public void Setup(CastleUpgradeData.Level lvl) { level = lvl; }

    public string DisplayName => level != null ? $"Upgrade: {level.displayName}" : "Upgrade";
    public string Description => level != null ? level.description : "";
    public int WoodCost => level != null ? level.woodCost : 0;
    public int MeatCost => level != null ? level.meatCost : 0;
    public int MoneyCost => level != null ? level.moneyCost : 0;
}

public class CastleMaxRecipe : IRecipe
{
    public string DisplayName => "Castle at max level";
    public string Description => "The castle has reached its final form.";
    public int WoodCost => 0;
    public int MeatCost => 0;
    public int MoneyCost => 0;
}

public class CastleEvolvingRecipe : IRecipe
{
    public string DisplayName => "Castle evolving";
    public string Description => "The crystal is growing. Protect the castle until it completes its transformation.";
    public int WoodCost => 0;
    public int MeatCost => 0;
    public int MoneyCost => 0;
}
}
