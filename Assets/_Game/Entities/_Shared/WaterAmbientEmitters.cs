using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Audio
{
public class WaterAmbientEmitters : MonoBehaviour
{
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private AudioClip clip;
    [Range(0f,1f)] [SerializeField] private float volume = 0.6f;
    [Tooltip("Distancia entre emisores en celdas. Mas alto = menos emisores.")]
    [SerializeField] private int gridSpacing = 8;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 14f;

    private void Start()
    {
        if (waterTilemap == null || clip == null) return;
        var bounds = waterTilemap.cellBounds;
        int created = 0;
        for (int x = bounds.xMin; x < bounds.xMax; x += gridSpacing) {
            for (int y = bounds.yMin; y < bounds.yMax; y += gridSpacing) {
                var cell = new Vector3Int(x, y, 0);
                if (!waterTilemap.HasTile(cell)) continue;
                Vector3 world = waterTilemap.CellToWorld(cell) + waterTilemap.tileAnchor;
                var go = new GameObject("WaterEmitter_" + x + "_" + y);
                go.transform.SetParent(transform, false);
                go.transform.position = world;
                var src = go.AddComponent<AudioSource>();
                src.clip = clip; src.loop = true; src.playOnAwake = false;
                src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = minDistance; src.maxDistance = maxDistance;
                src.volume = volume; src.dopplerLevel = 0f;
                src.Play();
                created++;
            }
        }
        Debug.Log("[WaterAmbientEmitters] Created " + created + " emitters");
    }
}
}
