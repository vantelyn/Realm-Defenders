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
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask treeLayer;
    [SerializeField] private string unitTag = "Unit";
    [SerializeField] private string buildingTag = "Building";

    private static readonly Collider2D[] overlapBuffer = new Collider2D[16];

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
        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(Input.mousePosition);

        ContactFilter2D debugFilter = new ContactFilter2D();
        debugFilter.SetLayerMask(selectableLayer);
        debugFilter.useLayerMask = true;
        debugFilter.useTriggers = true;
        int debugCount = Physics2D.OverlapPoint(mouseWorld, debugFilter, overlapBuffer);
        for (int i = 0; i < debugCount; i++)
        {
            if (overlapBuffer[i] != null)
                Debug.Log($"HOVER: {overlapBuffer[i].name} | tag={overlapBuffer[i].tag} | layer={LayerMask.LayerToName(overlapBuffer[i].gameObject.layer)} | trigger={overlapBuffer[i].isTrigger}");
        }

        mouseWorld.z = 0f;

        Collider2D enemyHit = Physics2D.OverlapPoint(mouseWorld, enemyLayer);
        if (enemyHit != null)
        {
            PlayerUnit selected = selectionManager != null ? selectionManager.SelectedUnit : null;
            if (selected is ArcherUnit) return CursorType.EnemyBow;
            if (selected is PawnScript) return CursorType.EnemyFist;
            return CursorType.Enemy;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(selectableLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int count = Physics2D.OverlapPoint(mouseWorld, filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = overlapBuffer[i];
            if (col == null) continue;

            if (col.CompareTag(buildingTag))
            {
                PlayerUnit selected = selectionManager != null ? selectionManager.SelectedUnit : null;
                if (selected == null) return CursorType.Default;

                Building b = col.GetComponentInParent<Building>();
                if (b == null) return CursorType.Default;

                if (selected.IsGarrisoned && selected.CurrentBuilding == b) return CursorType.Door;
                if (b.HasFreeSlot) return CursorType.Door;
                return CursorType.DoorBlocked;
            }

            if (col.CompareTag(unitTag))
            {
                PlayerUnit selected = selectionManager != null ? selectionManager.SelectedUnit : null;
                if (selected is MonkUnit monk)
                {
                    PlayerUnit hovered = col.GetComponentInParent<PlayerUnit>();
                    if (CanMonkHeal(monk, hovered)) return CursorType.Heal;
                }
                return CursorType.Ally;
            }
        }

        Collider2D treeHit = Physics2D.OverlapPoint(mouseWorld, treeLayer);
        if (treeHit != null) return CursorType.Wood;

        return CursorType.Default;
    }

    private bool CanMonkHeal(MonkUnit monk, PlayerUnit target)
    {
        if (target == null) return false;
        DamageReceiverPlayer hp = target.GetComponent<DamageReceiverPlayer>();
        if (hp == null) return false;
        if (hp.IsAtFullHealth) return false;
        float distance = Vector2.Distance(monk.transform.position, target.transform.position);
        if (distance > monk.HealRange) return false;
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