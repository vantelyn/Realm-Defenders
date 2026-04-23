using UnityEngine;

[CreateAssetMenu(fileName = "BuildingRecipe", menuName = "Game/Building Recipe")]
public class BuildingRecipe : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Tower";

    [Header("Cost")]
    public int woodCost = 0;
    public int meatCost = 0;
    public int moneyCost = 0;

    [Header("Prefabs")]
    [Tooltip("Prefab del edificio final que se instancia al construir.")]
    public GameObject buildingPrefab;

    [Tooltip("Prefab del ghost translúcido que sigue al ratón antes de colocar.")]
    public GameObject ghostPrefab;

    [Header("Footprint (fallback)")]
    [Tooltip("Tamaño de la zona bloqueante. Se usa solo si el prefab no tiene CapsuleCollider2D ni BoxCollider2D.")]
    public Vector2 footprintSize = new Vector2(1.6f, 1.6f);

    [Tooltip("Offset del centro del footprint respecto al pivote del ghost.")]
    public Vector2 footprintOffset = Vector2.zero;

    [Header("Identity")]
    public string displayName = "Tower";

    [TextArea(2, 4)]
    public string description = "";

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