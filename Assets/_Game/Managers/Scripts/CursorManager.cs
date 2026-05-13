using UnityEngine;
using Game.Buildings;
using Game.Combat;
using Game.Config;
using Game.Units;

namespace Game.Managers
{

public class CursorManager : MonoBehaviour
{
    public enum CursorType { Default, Ally, Heal, Enemy, EnemyBow, Hammer, Wood, Door, DoorBlocked, Demolish }

    [System.Serializable]
    public class CursorStyle
    {
        public CursorType type;
        public Texture2D texture;
        public Vector2 hotspot = Vector2.zero;
    }

    [SerializeField] private CursorStyle[] styles;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private TargetingConfig targeting;

    private CursorType currentType = CursorType.Default;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Start()
    {
        Apply(CursorType.Default);
    }

    private void Update()
    {
        CursorType desired = DetectContext();
        if (desired != currentType)
        {
            Apply(desired);
        }
    }

    private CursorType DetectContext()
    {
        if (targeting == null) return CursorType.Default;
        // Modo demoler: hammer sobre edificios demolibles, default fuera.
        var bm = BuildingManager.Instance;
        if (bm != null && bm.IsDemolishing)
        {
            Vector3 mw = worldCamera.ScreenToWorldPoint(Input.mousePosition); mw.z = 0f;
            int pbLayer = LayerMask.NameToLayer("PlayerBuilding");
            int mask = pbLayer >= 0 ? (1 << pbLayer) : 0;
            var hit = mask != 0 ? Physics2D.OverlapPoint(mw, mask) : null;
            return hit != null ? CursorType.Demolish : CursorType.Default;
        }

        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        PlayerUnit selected = selectionManager != null ? selectionManager.SelectedUnit : null;
        var sel = selectionManager != null ? selectionManager.SelectedUnits : null;

        // FutureKing seleccionado: hover sobre cualquier PlayerBuilding -> cursor de puerta para entrar.
        if (selected is Game.Units.FutureKing && !selected.IsGarrisoned)
        {
            int pbLayer = LayerMask.NameToLayer("PlayerBuilding");
            int mask = pbLayer >= 0 ? (1 << pbLayer) : 0;
            if (mask != 0 && Physics2D.OverlapPoint(mouseWorld, mask) != null) return CursorType.Door;
        }
        // Prioridad: enemigos.
        if (QueryService.HasHitAt(mouseWorld, targeting.enemyLayer))
        {
            if (selected is ArcherUnit) return CursorType.EnemyBow;
            return CursorType.Enemy;
        }

        // Prioridad: edificios.
        Building building = QueryService.FindBuildingAt(mouseWorld, targeting.buildingsLayer, targeting.buildingTag);
        if (building != null)
        {
            if (selected == null) return CursorType.Default;

            // Si hay alguna unidad garrisoned en la seleccion (en cualquier building),
            // el RMB las sacara: cursor de puerta (salida).
            if (sel != null)
            {
                for (int i = 0; i < sel.Count; i++)
                {
                    if (sel[i] != null && sel[i].IsGarrisoned) return CursorType.Door;
                }
            }

            // Sin garrisoned en seleccion: RMB intentaria meter. Door si el building tiene hueco.
            if (building.HasFreeSlot) return CursorType.Door;
            return CursorType.DoorBlocked;
        }

        // Prioridad: unidades aliadas.
        PlayerUnit hovered = QueryService.FindUnitAt(mouseWorld, targeting.unitsLayer, targeting.unitTag);
        if (hovered != null)
        {
            if (selected is MonkUnit monk && CanMonkHeal(monk, hovered)) return CursorType.Heal;
            return CursorType.Ally;
        }

        // Prioridad: arboles.
        if (QueryService.HasHitAt(mouseWorld, targeting.treeLayer))
        {
            return CursorType.Wood;
        }

        // Prioridad: ovejas.
        if (QueryService.HasHitAt(mouseWorld, targeting.sheepLayer))
        {
            return CursorType.Enemy;
        }

        return CursorType.Default;
    }

    private bool CanMonkHeal(MonkUnit monk, PlayerUnit target)
    {
        if (target == null) return false;
        DamageReceiverPlayer hp = target.GetComponent<DamageReceiverPlayer>();
        if (hp == null) return false;
        if (hp.IsAtFullHealth) return false;
        float distSqr = ((Vector2)monk.transform.position - (Vector2)target.transform.position).sqrMagnitude;
        if (distSqr > monk.HealRange * monk.HealRange) return false;
        return true;
    }

    private void Apply(CursorType type)
    {
        currentType = type;
        foreach (CursorStyle s in styles)
        {
            if (s.type == type && s.texture != null)
            {
                Cursor.SetCursor(s.texture, s.hotspot, CursorMode.Auto);
                return;
            }
        }
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
}