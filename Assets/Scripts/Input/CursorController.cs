using UnityEngine;

public class CursorController : MonoBehaviour
{
    public enum CursorType { Default, Ally, Heal, Enemy, EnemyBow, EnemyFist, Wood, Door, DoorBlocked }

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

        // Prioridad: enemigos.
        if (QueryService.HasHitAt(mouseWorld, targeting.enemyLayer))
        {
            PlayerUnit selected = selectionManager != null ? selectionManager.SelectedUnit : null;
            if (selected is ArcherUnit) return CursorType.EnemyBow;
            if (selected is PawnScript) return CursorType.EnemyFist;
            return CursorType.Enemy;
        }

        // Prioridad: edificios (puertas).
        Building building = QueryService.FindBuildingAt(mouseWorld, targeting.buildingsLayer, targeting.buildingTag);
        if (building != null)
        {
            PlayerUnit selected = selectionManager != null ? selectionManager.SelectedUnit : null;
            if (selected == null) return CursorType.Default;

            if (selected.IsGarrisoned && selected.CurrentBuilding == building) return CursorType.Door;
            if (building.HasFreeSlot) return CursorType.Door;
            return CursorType.DoorBlocked;
        }

        // Prioridad: unidades aliadas.
        PlayerUnit hovered = QueryService.FindUnitAt(mouseWorld, targeting.unitsLayer, targeting.unitTag);
        if (hovered != null)
        {
            PlayerUnit selected = selectionManager != null ? selectionManager.SelectedUnit : null;
            if (selected is MonkUnit monk && CanMonkHeal(monk, hovered)) return CursorType.Heal;
            return CursorType.Ally;
        }

        // Prioridad: árboles.
        if (QueryService.HasHitAt(mouseWorld, targeting.treeLayer)) return CursorType.Wood;

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