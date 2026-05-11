using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using NavMeshPlus.Components;
using Game.Core;
using Game.PlayerInput;

namespace Game.Buildings
{

public class BuildingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private SelectionManager selectionManager;

    [Header("Validation")]
    [Tooltip("Layers con los que el ghost no puede solapar (edificios, unidades, �rboles, agua...).")]
    [SerializeField] private LayerMask blockingLayers;

    [SerializeField] private float placementZ = 0f;

    [Header("Navigation")]
    [Tooltip("NavMeshSurface a rebakear tras construir un edificio. Si est� vac�o, no se rebake.")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    private BuildingRecipe activeRecipe;
    private GameObject ghostInstance;
    private BuildGhost ghostVisual;
    private bool canPlaceHere;

    private Vector2 currentFootprintSize;
    private Vector2 currentFootprintOffset;

    public bool IsPlacing => activeRecipe != null;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
    }

    public void BeginPlacement(BuildingRecipe recipe)
    {
        if (recipe == null || recipe.buildingPrefab == null) return;
        if (InventoryManager.Instance == null) return;

        if (!recipe.CanAfford(InventoryManager.Instance))
        {
            // TODO: feedback de "no tienes recursos".
            return;
        }

        CancelPlacement();

        activeRecipe = recipe;
        ResolveFootprint(recipe);

        if (recipe.ghostPrefab != null)
        {
            ghostInstance = Instantiate(recipe.ghostPrefab);
            ghostVisual = ghostInstance.GetComponent<BuildGhost>();
        }

        if (selectionManager != null) selectionManager.Deselect();
    }

    private void ResolveFootprint(BuildingRecipe recipe)
    {
        currentFootprintSize = recipe.footprintSize;
        currentFootprintOffset = recipe.footprintOffset;

        if (recipe.buildingPrefab == null) return;

        CapsuleCollider2D capsule = recipe.buildingPrefab.GetComponentInChildren<CapsuleCollider2D>();
        if (capsule != null)
        {
            currentFootprintSize = capsule.size;
            currentFootprintOffset = capsule.offset;
            return;
        }

        BoxCollider2D box = recipe.buildingPrefab.GetComponentInChildren<BoxCollider2D>();
        if (box != null)
        {
            currentFootprintSize = box.size;
            currentFootprintOffset = box.offset;
            return;
        }
    }

    public void CancelPlacement()
    {
        if (ghostInstance != null) Destroy(ghostInstance);
        ghostInstance = null;
        ghostVisual = null;
        activeRecipe = null;
        canPlaceHere = false;
    }

    private void Update()
    {
        if (!IsPlacing) return;

        if (Input.GetKeyDown(KeyCode.Escape) || (Input.GetMouseButtonDown(1) && !IsPointerOverUI()))
        {
            CancelPlacement();
            return;
        }

        UpdateGhostPosition();
        EvaluateValidity();

        if (Input.GetMouseButtonDown(0) && canPlaceHere && !IsPointerOverUI())
        {
            ConfirmPlacement();
        }
    }

    private void UpdateGhostPosition()
    {
        if (ghostInstance == null) return;
        Vector3 mouse = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = placementZ;
        ghostInstance.transform.position = mouse;
    }

    private void EvaluateValidity()
    {
        if (ghostInstance == null) return;

        Vector2 center = (Vector2)ghostInstance.transform.position + currentFootprintOffset;
        bool blocked = Physics2D.OverlapBox(center, currentFootprintSize, 0f, blockingLayers) != null;
        canPlaceHere = !blocked;

        if (ghostVisual != null) ghostVisual.SetValid(canPlaceHere);
    }

    private void ConfirmPlacement()
    {
        if (activeRecipe == null) return;

        if (!activeRecipe.TryPay(InventoryManager.Instance))
        {
            CancelPlacement();
            return;
        }

        Vector3 pos = ghostInstance.transform.position;
        Instantiate(activeRecipe.buildingPrefab, pos, Quaternion.identity);

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }

        CancelPlacement();
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void OnDrawGizmosSelected()
    {
        if (ghostInstance == null) return;
        Vector2 center = (Vector2)ghostInstance.transform.position + currentFootprintOffset;
        Gizmos.color = canPlaceHere ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, currentFootprintSize);
    }
}
}
