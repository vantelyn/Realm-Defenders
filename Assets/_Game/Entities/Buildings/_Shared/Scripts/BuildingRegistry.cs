using System;
using System.Collections.Generic;

namespace Game.Buildings
{

/// <summary>
/// Mantiene la lista de Building vivos por BuildingRecipe. Building.OnEnable
/// se registra y OnDisable se desregistra. OnChanged se dispara al cambiar.
/// </summary>
public static class BuildingRegistry
{
    private static readonly Dictionary<BuildingRecipe, List<Building>> live = new Dictionary<BuildingRecipe, List<Building>>();

    public static event Action OnChanged;

    public static void Register(BuildingRecipe recipe, Building b)
    {
        if (recipe == null || b == null) return;
        List<Building> list;
        if (!live.TryGetValue(recipe, out list)) { list = new List<Building>(); live[recipe] = list; }
        if (!list.Contains(b)) list.Add(b);
        if (OnChanged != null) OnChanged();
    }

    public static void Unregister(BuildingRecipe recipe, Building b)
    {
        if (recipe == null) return;
        List<Building> list;
        if (!live.TryGetValue(recipe, out list)) return;
        list.Remove(b);
        if (list.Count == 0) live.Remove(recipe);
        if (OnChanged != null) OnChanged();
    }

    public static int Count(BuildingRecipe recipe)
    {
        if (recipe == null) return 0;
        List<Building> list;
        return live.TryGetValue(recipe, out list) ? list.Count : 0;
    }

    public static bool IsBuilt(BuildingRecipe recipe) => Count(recipe) > 0;

    /// <summary>Primer Building vivo de ese recipe, o null. Util para que SpawnButton
    /// resuelva el UnitSpawner en runtime sin necesidad de referencias serializadas.</summary>
    public static Building GetFirst(BuildingRecipe recipe)
    {
        if (recipe == null) return null;
        List<Building> list;
        if (!live.TryGetValue(recipe, out list)) return null;
        for (int i = 0; i < list.Count; i++) if (list[i] != null) return list[i];
        return null;
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        live.Clear();
        OnChanged = null;
    }
}
}
