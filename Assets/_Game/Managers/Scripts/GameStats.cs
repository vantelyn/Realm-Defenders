using System;
using UnityEngine;

namespace Game.Managers
{

/// <summary>
/// Contador global de estadisticas de la partida actual. Se resetea automaticamente
/// al cargar la escena (RuntimeInitializeOnLoadMethod). Los puntos de tracking
/// llaman a Notify*() y la pantalla de victoria los lee al final.
/// </summary>
public static class GameStats
{
    public static int EnemiesKilled { get; private set; }
    public static int WoodCollected { get; private set; }
    public static int MeatCollected { get; private set; }
    public static int MoneyCollected { get; private set; }
    public static int AlliesLost { get; private set; }
    public static int BuildingsLost { get; private set; }

    public static event Action OnChanged;

    public static void NotifyEnemyKilled() { EnemiesKilled++; OnChanged?.Invoke(); }
    public static void NotifyWoodCollected(int amount) { WoodCollected += amount; OnChanged?.Invoke(); }
    public static void NotifyMeatCollected(int amount) { MeatCollected += amount; OnChanged?.Invoke(); }
    public static void NotifyMoneyCollected(int amount) { MoneyCollected += amount; OnChanged?.Invoke(); }
    public static void NotifyAllyLost() { AlliesLost++; OnChanged?.Invoke(); }
    public static void NotifyBuildingLost() { BuildingsLost++; OnChanged?.Invoke(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        EnemiesKilled = 0;
        WoodCollected = 0;
        MeatCollected = 0;
        MoneyCollected = 0;
        AlliesLost = 0;
        BuildingsLost = 0;
        OnChanged = null;
    }
}
}
