using UnityEngine;
using Game.Core;

namespace Game.Buildings
{

[CreateAssetMenu(fileName = "UnitRecipe", menuName = "Game/Unit Recipe")]
public class UnitRecipe : ScriptableObject, IRecipe
{
    [Header("Identity")]
    public string displayName = "Warrior";

    [TextArea(2, 4)]
    public string description = "";

    [Header("Cost")]
    public int woodCost = 0;
    public int meatCost = 0;
    public int moneyCost = 0;

    [Header("Prefab")]
    public GameObject unitPrefab;

    public string DisplayName => displayName;
    public string Description => description;
    public int WoodCost => woodCost;
    public int MeatCost => meatCost;
    public int MoneyCost => moneyCost;

    public bool CanAfford(PlayerInventory inv)
    {
        return inv.Wood >= woodCost
            && inv.Meat >= meatCost
            && inv.Money >= moneyCost;
    }

    public bool TryPay(PlayerInventory inv)
    {
        if (!CanAfford(inv)) return false;

        if (woodCost > 0) inv.TrySpendWood(woodCost);
        if (meatCost > 0) inv.TrySpendMeat(meatCost);
        if (moneyCost > 0) inv.TrySpendMoney(moneyCost);
        return true;
    }
}
}
