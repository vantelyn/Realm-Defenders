using System.Collections.Generic;
using UnityEngine;
using Game.Targeting;

namespace Game.Managers
{

/// <summary>
/// Singleton MonoBehaviour del registro central de StrategicTarget vivos. Los
/// enemigos lo consultan para decidir a qué ir cuando no tienen objetivo de
/// oportunidad; la UI lo lee para mostrar amenaza acumulada.
///
/// Mantiene la API pública estática para que los consumidores no necesiten
/// conocer la instancia. Si la instancia no está activa (init order, escena
/// sin _ThreatManager), las llamadas son no-op seguras y los getters devuelven
/// valores neutros.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class ThreatManager : MonoBehaviour
{
    private static ThreatManager instance;
    public static ThreatManager Instance => instance;

    private readonly List<StrategicTarget> targets = new List<StrategicTarget>();

    public static event System.Action OnRegistryChanged;

    public static IReadOnlyList<StrategicTarget> All =>
        instance != null ? (IReadOnlyList<StrategicTarget>)instance.targets : System.Array.Empty<StrategicTarget>();
    public static int Count => instance != null ? instance.targets.Count : 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        instance.targets.Clear();
        instance = null;
    }

    public static void Register(StrategicTarget target)
    {
        if (instance == null || target == null) return;
        if (instance.targets.Contains(target)) return;
        instance.targets.Add(target);
        OnRegistryChanged?.Invoke();
    }

    public static void Unregister(StrategicTarget target)
    {
        if (instance == null || target == null) return;
        if (instance.targets.Remove(target))
        {
            OnRegistryChanged?.Invoke();
        }
    }

    public static void NotifyThreatLevelChanged(StrategicTarget target)
    {
        if (instance == null || target == null) return;
        if (!instance.targets.Contains(target)) return;
        OnRegistryChanged?.Invoke();
    }

    /// <summary>
    /// Objetivo con mayor threatLevel. Si hay empate, devuelve uno al azar
    /// entre los empatados (distribuye a los enemigos y evita que todos vayan
    /// al mismo).
    /// </summary>
    public static StrategicTarget GetTopThreat()
    {
        if (instance == null) return null;
        var list = instance.targets;
        float bestLevel = float.NegativeInfinity;
        int tieCount = 0;

        for (int i = 0; i < list.Count; i++)
        {
            StrategicTarget t = list[i];
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

        int pick = Random.Range(0, tieCount);
        int seen = 0;
        for (int i = 0; i < list.Count; i++)
        {
            StrategicTarget t = list[i];
            if (t == null) continue;
            if (Mathf.Approximately(t.ThreatLevel, bestLevel))
            {
                if (seen == pick) return t;
                seen++;
            }
        }
        return null;
    }

    /// <summary>Suma total de amenaza. Útil para calcular ritmo de spawn de portales.</summary>
    public static float GetTotalThreat()
    {
        if (instance == null) return 0f;
        float total = 0f;
        var list = instance.targets;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null) total += list[i].ThreatLevel;
        }
        return total;
    }

    /// <summary>Limpia el registro manualmente. Normalmente no es necesario: OnDestroy lo hace al recargar escena.</summary>
    public static void Clear()
    {
        if (instance == null) return;
        instance.targets.Clear();
        OnRegistryChanged?.Invoke();
    }
}

}
