using UnityEngine;
using Game.Buildings;
using Game.Combat;
using Game.Config;
using Game.Units;

namespace Game.Managers
{

public class CursorManager : MonoBehaviour
{
    public enum CursorType { Default, Ally, Heal, Enemy, EnemyBow, Hammer, Wood, Door, DoorBlocked }

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

        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        PlayerUnit selected = selectionManager != null ? selectionManager.SelectedUnit : null;

        // Prioridad: enemigos.
        if (QueryService.HasHitAt(mouseWorld, targeting.enemyLayer))
        {
            if (selected is PawnUnit) return CursorType.Enemy;
            if (selected is ArcherUnit) return CursorType.EnemyBow;
            return CursorType.Enemy;
        }

        // Prioridad: edificios.
        Building building = QueryService.FindBuildingAt(mouseWorld, targeting.buildingsLayer, targeting.buildingTag);
        if (building != null)
        {
            if (selected == null) return CursorType.Default;

            if (selected.IsGarrisoned && selected.CurrentBuilding == building) return CursorType.Door;
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

        // Prioridad: �rboles.
        if (QueryService.HasHitAt(mouseWorld, targeting.treeLayer))
        {
            if (selected is PawnUnit) return CursorType.Wood;
            return CursorType.Wood;
        }

        // Prioridad: ovejas.
        if (QueryService.HasHitAt(mouseWorld, targeting.sheepLayer))
        {
            if (selected is PawnUnit) return CursorType.Enemy;

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
