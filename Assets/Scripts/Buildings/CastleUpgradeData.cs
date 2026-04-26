using UnityEngine;

namespace Game.Buildings
{

[CreateAssetMenu(fileName = "CastleUpgradeData", menuName = "Game/Castle Upgrade Data")]
public class CastleUpgradeData : ScriptableObject
{
    [System.Serializable]
    public class Level
    {
        public string displayName = "Castle";
        [TextArea(2, 4)] public string description = "";

        [Header("Cost (para llegar a ESTE nivel)")]
        public int woodCost = 0;
        public int meatCost = 0;
        public int moneyCost = 0;

        [Header("Stats")]
        public int maxHealth = 200;
        public float threatLevel = 15f;
    }

    [Header("Levels (1-indexed: [0]=Lv1, [1]=Lv2, [2]=Lv3)")]
    public Level[] levels = new Level[3];

    [Header("Auto-evolution Lv2 → Lv3")]
    [Tooltip("Tiempo en segundos que tarda el castillo de nivel 2 en evolucionar al nivel 3.")]
    public float evolutionDuration = 60f;

    public Level GetLevel(int lv)
    {
        int idx = Mathf.Clamp(lv - 1, 0, levels.Length - 1);
        return levels[idx];
    }

    public bool HasLevel(int lv) => lv >= 1 && lv <= levels.Length;
    public int MaxLevel => levels.Length;
}
}
