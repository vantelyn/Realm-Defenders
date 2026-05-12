using UnityEngine;
using Game.Buildings;
using Game.Units;

namespace Game.Combat
{

/// <summary>
/// B�squedas de Physics2D centralizadas. Cachea buffer y filtros para evitar allocs por frame.
/// Todos los consumidores del juego (SelectionManager, CursorManager, AIs) pasan por aqu�.
/// </summary>
public static class QueryService
{
    private const int BufferSize = 64;
    private static readonly Collider2D[] buffer = new Collider2D[BufferSize];

    // ---- Point queries ----

    /// <summary>Busca una PlayerUnit en el punto. Filtra por layer + tag, y devuelve la primera v�lida.</summary>
    public static PlayerUnit FindUnitAt(Vector2 worldPoint, LayerMask layer, string tag)
    {
        ContactFilter2D filter = BuildFilter(layer);
        int count = Physics2D.OverlapPoint(worldPoint, filter, buffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = buffer[i];
            if (col == null) continue;
            if (!col.CompareTag(tag)) continue;
            PlayerUnit u = col.GetComponentInParent<PlayerUnit>();
            if (u != null) return u;
        }
        return null;
    }

    /// <summary>Busca un Building en el punto.</summary>
    public static Building FindBuildingAt(Vector2 worldPoint, LayerMask layer, string tag)
    {
        ContactFilter2D filter = BuildFilter(layer);
        int count = Physics2D.OverlapPoint(worldPoint, filter, buffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = buffer[i];
            if (col == null) continue;
            if (!col.CompareTag(tag)) continue;
            Building b = col.GetComponentInParent<Building>();
            if (b != null) return b;
        }
        return null;
    }

    /// <summary>
    /// Versi�n gen�rica para simple hit en layer (sin filtro de tag).
    /// Devuelve true si hay alg�n collider.
    /// </summary>
    public static bool HasHitAt(Vector2 worldPoint, LayerMask layer)
    {
        return Physics2D.OverlapPoint(worldPoint, layer) != null;
    }

    // ---- Radius queries ----

    /// <summary>Busca el Building libre m�s cercano a origin dentro del radio.</summary>
    public static Building FindClosestFreeBuilding(Vector3 origin, float radius, LayerMask layer, string tag)
    {
        ContactFilter2D filter = BuildFilter(layer);
        int count = Physics2D.OverlapCircle(origin, radius, filter, buffer);
        Building best = null;
        float bestDistSqr = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = buffer[i];
            if (col == null) continue;
            if (!col.CompareTag(tag)) continue;
            Building b = col.GetComponentInParent<Building>();
            if (b == null || !b.HasFreeSlot) continue;
            float distSqr = ((Vector2)origin - (Vector2)b.DoorPosition).sqrMagnitude;
            if (distSqr < bestDistSqr) { bestDistSqr = distSqr; best = b; }
        }
        return best;
    }

    /// <summary>Busca todas las PlayerUnit dentro del rectangulo world-space [min, max].
    /// Filtra por layer + tag; excluye unidades garrisoned (no son seleccionables por box).
    /// Rellena 'results' (limpia previamente) y devuelve el numero de hits unicos.</summary>
    public static int FindUnitsInBox(Vector2 worldMin, Vector2 worldMax, LayerMask layer, string tag, System.Collections.Generic.List<PlayerUnit> results)
    {
        if (results == null) return 0;
        results.Clear();
        Vector2 size = new Vector2(Mathf.Abs(worldMax.x - worldMin.x), Mathf.Abs(worldMax.y - worldMin.y));
        if (size.x <= 0f || size.y <= 0f) return 0;

        ContactFilter2D filter = BuildFilter(layer);
        Vector2 center = (worldMin + worldMax) * 0.5f;
        int count = Physics2D.OverlapBox(center, size, 0f, filter, buffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = buffer[i];
            if (col == null) continue;
            if (!col.CompareTag(tag)) continue;
            PlayerUnit u = col.GetComponentInParent<PlayerUnit>();
            if (u == null) continue;
            if (results.Contains(u)) continue; // dedup colliders del mismo unit
            results.Add(u);
        }
        return results.Count;
    }


    // ---- Helpers internos ----

    private static ContactFilter2D BuildFilter(LayerMask layer)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(layer);
        filter.useLayerMask = true;
        filter.useTriggers = true;
        return filter;
    }
}
}
