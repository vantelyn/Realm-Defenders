using UnityEngine;
using Game.Core;
using Game.Managers;

namespace Game.Buildings
{

[CreateAssetMenu(fileName = "BuildingRecipe", menuName = "Game/Building Recipe")]
public class BuildingRecipe : ScriptableObject, IRecipe
{
    [Header("Identity")]
    public string displayName = "Tower";

    [TextArea(2, 4)]
    public string description = "";

    [Header("Cost")]
    public int woodCost = 0;
    public int meatCost = 0;
    public int moneyCost = 0;

    [Header("Prefabs")]
    [Tooltip("Prefab del edificio final que se instancia al construir.")]
    public GameObject buildingPrefab;

    [Tooltip("Prefab del ghost transl�cido que sigue al rat�n antes de colocar.")]
    public GameObject ghostPrefab;

    [Header("Footprint (fallback)")]
    [Tooltip("Tama�o de la zona bloqueante. Se usa solo si el prefab no tiene CapsuleCollider2D ni BoxCollider2D.")]
    public Vector2 footprintSize = new Vector2(1.6f, 1.6f);

    [Tooltip("Offset del centro del footprint respecto al pivote del ghost.")]
    public Vector2 footprintOffset = Vector2.zero;

    public string DisplayName => displayName;
    public string Description => description;
    public int WoodCost => woodCost;
    public int MeatCost => meatCost;
    public int MoneyCost => moneyCost;

    public bool CanAfford(InventoryManager inv)
    {
        return inv.Wood >= woodCost
            && inv.Meat >= meatCost
            && inv.Money >= moneyCost;
    }

    public bool TryPay(InventoryManager inv)
    {
        if (!CanAfford(inv)) return false;

        if (woodCost > 0) inv.TrySpendWood(woodCost);
        if (meatCost > 0) inv.TrySpendMeat(meatCost);
        if (moneyCost > 0) inv.TrySpendMoney(moneyCost);
        return true;
    }
}
}
