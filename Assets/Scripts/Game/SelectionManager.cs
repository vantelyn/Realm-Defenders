using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private CameraFollowController cameraFollow;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask selectableLayerMask;
    [SerializeField] private string unitTag = "Unit";
    [SerializeField] private string buildingTag = "Building";
    [SerializeField] private KeyCode selectionKey = KeyCode.F;

    private static readonly Collider2D[] overlapBuffer = new Collider2D[16];

    private PlayerUnit selectedUnit;
    public PlayerUnit SelectedUnit => selectedUnit;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
            return;
        }

        if (Input.GetKeyDown(selectionKey)) HandleSelectionKey();
        else if (Input.GetMouseButtonDown(0)) HandleLeftClick();
        else if (Input.GetMouseButtonDown(1)) HandleSecondaryAction();
    }

    private void HandleSelectionKey()
    {
        if (IsPointerOverUI()) return;

        Vector3 mouseWorld = GetMouseWorld();
        PlayerUnit hit = GetUnitAt(mouseWorld);

        if (hit != null && hit != selectedUnit)
        {
            Select(hit);
        }
        else if (selectedUnit != null)
        {
            Deselect();
        }
    }

    private void HandleLeftClick()
    {
        if (IsPointerOverUI()) return;

        Vector3 mouseWorld = GetMouseWorld();

        // Prioridad 1: puerta de edificio con unidad seleccionada.
        Building building = GetBuildingAt(mouseWorld);
        if (building != null && selectedUnit != null)
        {
            if (selectedUnit.IsGarrisoned && selectedUnit.CurrentBuilding == building)
            {
                building.Exit(selectedUnit);
                return;
            }
            if (!selectedUnit.IsGarrisoned && building.HasFreeSlot)
            {
                building.TryEnter(selectedUnit);
                return;
            }
            return;
        }

        // Prioridad 2: hover sobre unidad seleccionable → seleccionar / cambiar.
        PlayerUnit hit = GetUnitAt(mouseWorld);
        if (hit != null)
        {
            if (hit != selectedUnit) Select(hit);
            return;
        }

        // Prioridad 3: primary attack.
        if (selectedUnit == null) return;
        Vector2 aim = (Vector2)(mouseWorld - selectedUnit.transform.position);
        selectedUnit.PrimaryAttack(aim);
    }

    private void HandleSecondaryAction()
    {
        if (IsPointerOverUI()) return;
        if (selectedUnit == null) return;
        if (!selectedUnit.HasSecondary) return;

        Vector3 mouseWorld = GetMouseWorld();
        PlayerUnit hit = GetUnitAt(mouseWorld);

        if (hit != null && hit != selectedUnit && !selectedUnit.SecondaryTargetsAllies)
        {
            return;
        }

        Vector2 aim = (Vector2)(mouseWorld - selectedUnit.transform.position);
        selectedUnit.SecondaryAction(aim, hit);
    }

    private Vector3 GetMouseWorld()
    {
        Vector3 p = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }

    private PlayerUnit GetUnitAt(Vector2 worldPoint)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(selectableLayerMask);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int count = Physics2D.OverlapPoint(worldPoint, filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = overlapBuffer[i];
            if (col == null) continue;
            if (!col.CompareTag(unitTag)) continue;
            PlayerUnit u = col.GetComponentInParent<PlayerUnit>();
            if (u != null) return u;
        }
        return null;
    }

    private Building GetBuildingAt(Vector2 worldPoint)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(selectableLayerMask);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int count = Physics2D.OverlapPoint(worldPoint, filter, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = overlapBuffer[i];
            if (col == null) continue;
            if (!col.CompareTag(buildingTag)) continue;
            Building b = col.GetComponentInParent<Building>();
            if (b != null) return b;
        }
        return null;
    }

    private void Select(PlayerUnit unit)
    {
        if (selectedUnit != null) selectedUnit.SetSelected(false);
        selectedUnit = unit;
        selectedUnit.SetSelected(true);
        if (cameraFollow != null) cameraFollow.SetFollowTarget(unit.transform);
    }

    private void Deselect()
    {
        if (selectedUnit == null) return;
        selectedUnit.SetSelected(false);
        selectedUnit = null;
        if (cameraFollow != null) cameraFollow.SetFollowTarget(null);
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}