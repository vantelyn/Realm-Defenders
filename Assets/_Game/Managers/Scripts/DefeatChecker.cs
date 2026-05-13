using UnityEngine;
using Game.Buildings;
using Game.Units;

namespace Game.Managers
{
public class DefeatChecker : MonoBehaviour
{
    [SerializeField] private UnitRecipe[] unitRecipes;
    [SerializeField] private BuildingRecipe[] buildingRecipes;
    [SerializeField] private float checkInterval = 2f;
    public static event System.Action<float> OnDefeat;
    private float startTime;
    private float nextCheck;
    private bool fired;
    private void Start() { startTime = Time.unscaledTime; nextCheck = Time.unscaledTime + 5f; }
    private void Update() {
        if (fired) return;
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + checkInterval;
        if (!IsDefeated()) return;
        fired = true;
        float duration = Time.unscaledTime - startTime;
        if (OnDefeat != null) OnDefeat(duration);
    }
    private bool IsDefeated() {
        // 1) Hay PlayerUnit viva? Si si -> no defeat.
        var units = Object.FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None);
        if (units != null && units.Length > 0) return false;
        var inv = InventoryManager.Instance;
        if (inv == null) return false;
        // 2) Alguna UnitRecipe spawneable ahora?
        if (unitRecipes != null) {
            foreach (var u in unitRecipes) {
                if (u == null) continue;
                if (u.requiredBuilding != null && !BuildingRegistry.IsBuilt(u.requiredBuilding)) continue;
                if (u.CanAfford(inv)) return false;
            }
        }
        // 3) Alguna BuildingRecipe asequible (recovery via construir)?
        if (buildingRecipes != null) {
            foreach (var b in buildingRecipes) {
                if (b == null) continue;
                if (b.isUnique && BuildingRegistry.IsBuilt(b)) continue;
                if (b.CanAfford(inv)) return false;
            }
        }
        return true;
    }
}
}
