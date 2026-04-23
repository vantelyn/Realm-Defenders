using UnityEngine;

/// <summary>
/// Debug temporal para inspeccionar ThreatRegistry en runtime.
/// Muestra en pantalla el top threat, el total y la lista completa.
/// Borrar cuando no haga falta.
/// </summary>
public class ThreatRegistryDebug : MonoBehaviour
{
    [SerializeField] private bool showOnScreen = true;
    [SerializeField] private KeyCode logToConsoleKey = KeyCode.T;

    private void Update()
    {
        if (Input.GetKeyDown(logToConsoleKey))
        {
            LogSnapshot();
        }
    }

    private void LogSnapshot()
    {
        StrategicTarget top = ThreatRegistry.GetTopThreat();
        Debug.Log($"=== ThreatRegistry snapshot ===");
        Debug.Log($"Count: {ThreatRegistry.Count}");
        Debug.Log($"Total threat: {ThreatRegistry.GetTotalThreat():F2}");
        Debug.Log($"Top threat: {(top != null ? $"{top.name} ({top.ThreatLevel:F2})" : "NONE")}");
        Debug.Log($"--- All targets ---");
        foreach (StrategicTarget t in ThreatRegistry.All)
        {
            if (t == null) continue;
            Debug.Log($"  - {t.name}: threat={t.ThreatLevel:F2}");
        }
    }

    private void OnGUI()
    {
        if (!showOnScreen) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };
        GUIStyle bgStyle = new GUIStyle(GUI.skin.box);

        StrategicTarget top = ThreatRegistry.GetTopThreat();
        string topLabel = top != null ? $"{top.name} ({top.ThreatLevel:F2})" : "NONE";

        float y = 10f;
        GUI.Box(new Rect(10, y, 320, 50), "");
        GUI.Label(new Rect(20, y + 5, 300, 20), $"Threat count: {ThreatRegistry.Count}   total: {ThreatRegistry.GetTotalThreat():F2}", style);
        GUI.Label(new Rect(20, y + 25, 300, 20), $"Top threat: {topLabel}", style);

        y += 60f;
        int lineCount = Mathf.Min(ThreatRegistry.Count, 15);
        GUI.Box(new Rect(10, y, 320, 20 + lineCount * 18), "");
        GUI.Label(new Rect(20, y + 3, 300, 20), "Targets:", style);
        int shown = 0;
        foreach (StrategicTarget t in ThreatRegistry.All)
        {
            if (t == null) continue;
            if (shown >= 15) break;
            GUI.Label(new Rect(20, y + 20 + shown * 18, 300, 20), $"  {t.name}: {t.ThreatLevel:F2}", style);
            shown++;
        }
    }
}