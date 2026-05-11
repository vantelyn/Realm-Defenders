using UnityEngine;
using UnityEngine.EventSystems;
using Game.Buildings;
using Game.CameraSystem;
using Game.Config;
using Game.Core;
using Game.Units;

namespace Game.Managers
{

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private CameraFollowController cameraFollow;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private TargetingConfig targeting;
    [SerializeField] private KeyCode selectionKey = KeyCode.F;
    [SerializeField] private BuildingManager buildPlacer;

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
        if (buildPlacer != null && buildPlacer.IsPlacing) return;
        if (IsPointerOverUI()) return;
        if (targeting == null) return;

        Vector3 mouseWorld = GetMouseWorld();
        PlayerUnit hit = QueryService.FindUnitAt(mouseWorld, targeting.unitsLayer, targeting.unitTag);

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
        if (buildPlacer != null && buildPlacer.IsPlacing) return;
        if (IsPointerOverUI()) return;
        if (targeting == null) return;

        Vector3 mouseWorld = GetMouseWorld();

        Building building = QueryService.FindBuildingAt(mouseWorld, targeting.buildingsLayer, targeting.buildingTag);
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

        PlayerUnit hit = QueryService.FindUnitAt(mouseWorld, targeting.unitsLayer, targeting.unitTag);
        if (hit != null)
        {
            if (hit != selectedUnit) Select(hit);
            return;
        }

        if (selectedUnit == null) return;
        Vector2 aim = (Vector2)(mouseWorld - selectedUnit.transform.position);
        selectedUnit.PrimaryAttack(aim);
    }

    private void HandleSecondaryAction()
    {
        if (buildPlacer != null && buildPlacer.IsPlacing) return;
        if (IsPointerOverUI()) return;
        if (selectedUnit == null) return;
        if (!selectedUnit.HasSecondary) return;
        if (targeting == null) return;

        Vector3 mouseWorld = GetMouseWorld();
        PlayerUnit hit = QueryService.FindUnitAt(mouseWorld, targeting.unitsLayer, targeting.unitTag);

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

    private void Select(PlayerUnit unit)
    {
        if (selectedUnit != null) selectedUnit.SetSelected(false);
        selectedUnit = unit;
        selectedUnit.SetSelected(true);
        if (cameraFollow != null) cameraFollow.SetFollowTarget(unit.transform);
    }

    public void Deselect()
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
}
