using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private CameraFollowController cameraFollow;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private string playerUnitTag = "PlayerUnit";

    private PlayerUnit selectedUnit;

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

        if (Input.GetMouseButtonDown(0)) HandleLeftClick();
        else if (Input.GetMouseButtonDown(1)) HandleRightClick();
    }

    private void HandleLeftClick()
    {
        if (IsPointerOverUI()) return;

        Vector3 mouseWorld = GetMouseWorld();
        PlayerUnit hit = GetUnitAt(mouseWorld);

        if (hit != null && hit != selectedUnit)
        {
            Select(hit);
            return;
        }

        if (selectedUnit == null) return;
        Vector2 aim = (Vector2)(mouseWorld - selectedUnit.transform.position);
        selectedUnit.PrimaryAttack(aim);
    }

    private void HandleRightClick()
    {
        if (IsPointerOverUI()) return;

        Vector3 mouseWorld = GetMouseWorld();
        PlayerUnit hit = GetUnitAt(mouseWorld);

        if (hit != null && hit != selectedUnit) return;

        if (selectedUnit == null) return;
        if (!selectedUnit.HasSecondary) return;

        Vector2 aim = (Vector2)(mouseWorld - selectedUnit.transform.position);
        selectedUnit.SecondaryAction(aim);
    }

    private Vector3 GetMouseWorld()
    {
        Vector3 p = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }

    private PlayerUnit GetUnitAt(Vector2 worldPoint)
    {
        Collider2D col = Physics2D.OverlapPoint(worldPoint, playerLayerMask);
        if (col == null) return null;
        if (!col.CompareTag(playerUnitTag)) return null;
        return col.GetComponentInParent<PlayerUnit>();
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