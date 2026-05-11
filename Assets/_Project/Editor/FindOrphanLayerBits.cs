using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EditorTools
{

/// <summary>
/// Escanea LayerMasks serializadas en busca de bits que apuntan a slots de layer
/// sin nombre (huerfanos tras un rename/eliminacion). Ignora la mascara
/// "Everything" (-1) para evitar falsos positivos.
///
/// Uso: menu Tools > Find Orphan Layer Bits.
/// </summary>
public static class FindOrphanLayerBits
{
    [MenuItem("Tools/Find Orphan Layer Bits")]
    public static void Run()
    {
        var orphans = new List<int>();
        for (int i = 0; i < 32; i++)
        {
            if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) orphans.Add(i);
        }

        if (orphans.Count == 0)
        {
            Debug.Log("[FindOrphanLayerBits] No hay slots de layer sin nombre — nada que escanear.");
            return;
        }

        var report = new StringBuilder();
        int hits = 0;

        // Prefabs bajo Assets/
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (var g in prefabGuids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null) continue;
            foreach (var comp in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null) continue;
                ScanObject(comp, p + " :: " + comp.GetType().Name + " on '" + comp.gameObject.name + "'", orphans, report, ref hits);
            }
        }

        // ScriptableObjects bajo Assets/
        string[] soGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
        foreach (var g in soGuids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(p);
            if (asset == null) continue;
            ScanObject(asset, p + " | (SO " + asset.GetType().Name + ")", orphans, report, ref hits);
        }

        // Escenas cargadas
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.IsValid() || !scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var comp in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (comp == null) continue;
                    ScanObject(comp, "[Scene:" + scene.name + "] " + HierarchyPath(comp.transform) + " :: " + comp.GetType().Name, orphans, report, ref hits);
                }
            }
        }

        string header = hits == 0
            ? "Slots sin nombre: [" + string.Join(", ", orphans.ConvertAll(b => b.ToString()).ToArray()) + "]\nNingun bit huerfano en LayerMasks de gameplay.\n"
            : "Slots sin nombre: [" + string.Join(", ", orphans.ConvertAll(b => b.ToString()).ToArray()) + "]\nEncontradas " + hits + " referencia(s) con bit huerfano (ignorando mascaras 'Everything'):\n\n";

        Debug.Log("[FindOrphanLayerBits]\n" + header + report);
    }

    private static void ScanObject(Object target, string label, List<int> orphans, StringBuilder report, ref int hits)
    {
        var so = new SerializedObject(target);
        var it = so.GetIterator();
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.LayerMask) continue;
            int v = it.intValue;
            if (v == -1) continue;
            foreach (int bit in orphans)
            {
                if (((v >> bit) & 1) != 0)
                {
                    report.AppendLine(label + " :: " + it.propertyPath + " tiene bit huerfano " + bit + " (mask=" + v + ")");
                    hits++;
                    break;
                }
            }
        }
    }

    private static string HierarchyPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return HierarchyPath(t.parent) + "/" + t.name;
    }
}

}
