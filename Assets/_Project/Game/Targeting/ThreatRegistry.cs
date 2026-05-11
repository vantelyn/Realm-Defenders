using System.Collections.Generic;
using UnityEngine;

namespace Game.Targeting
{

/// <summary>
/// Registro central de todos los StrategicTarget vivos. Los enemigos lo
/// consultan para decidir a qu� ir cuando no tienen objetivo de oportunidad.
/// </summary>
public static class ThreatRegistry
{
    private static readonly List<StrategicTarget> targets = new List<StrategicTarget>();

    public static event System.Action OnRegistryChanged;

    public static IReadOnlyList<StrategicTarget> All => targets;
    public static int Count => targets.Count;

    public static void Register(StrategicTarget target)
    {
        if (target == null || targets.Contains(target)) return;
        targets.Add(target);
        OnRegistryChanged?.Invoke();
    }

    public static void Unregister(StrategicTarget target)
    {
        if (target == null) return;
        if (targets.Remove(target))
        {
            OnRegistryChanged?.Invoke();
        }
    }

    public static void NotifyThreatLevelChanged(StrategicTarget target)
    {
        if (target == null || !targets.Contains(target)) return;
        OnRegistryChanged?.Invoke();
    }

    /// <summary>
    /// Objetivo con mayor threatLevel. Si hay empate, devuelve uno al azar
    /// entre los empatados (distribuye a los enemigos y evita que todos vayan
    /// al mismo).
    /// </summary>
    public static StrategicTarget GetTopThreat()
    {
        float bestLevel = float.NegativeInfinity;
        int tieCount = 0;

        // Primera pasada: encontrar el m�ximo y contar empates.
        for (int i = 0; i < targets.Count; i++)
        {
            StrategicTarget t = targets[i];
            if (t == null) continue;
            if (t.ThreatLevel > bestLevel)
            {
                bestLevel = t.ThreatLevel;
                tieCount = 1;
            }
            else if (Mathf.Approximately(t.ThreatLevel, bestLevel))
            {
                tieCount++;
            }
        }

        if (tieCount == 0) return null;

        // Segunda pasada: elegir aleatorio entre los empatados.
        int pick = Random.Range(0, tieCount);
        int seen = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            StrategicTarget t = targets[i];
            if (t == null) continue;
            if (Mathf.Approximately(t.ThreatLevel, bestLevel))
            {
                if (seen == pick) return t;
                seen++;
            }
        }
        return null;
    }

    /// <summary>Suma total de amenaza. �til para calcular ritmo de spawn de portales.</summary>
    public static float GetTotalThreat()
    {
        float total = 0f;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null) total += targets[i].ThreatLevel;
        }
        return total;
    }

    /// <summary>Limpia el registro (�til al cargar escena).</summary>
    public static void Clear()
    {
        targets.Clear();
        OnRegistryChanged?.Invoke();
    }
}
}
