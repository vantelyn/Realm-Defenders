using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using NavMeshPlus.Components;
using Game.Buildings;

namespace Game.Managers
{

[DefaultExecutionOrder(-100)]
public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }
    [Header("Build SFX")]
    [SerializeField] private AudioClip buildSfxClip;
    [Range(0f,1f)] [SerializeField] private float buildSfxVolume = 0.9f;

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

    [Header("Demolish")]
    [Tooltip("Layer del root de los edificios destruibles (no incluye Castle si Castle no esta en esta layer).")]
    [SerializeField] private LayerMask demolishableLayers;
    [SerializeField] private AudioClip[] demolishSfxClips;
    [Range(0f,1f)] [SerializeField] private float demolishSfxVolume = 0.9f;
    private bool isDemolishing;
    private bool demolishSticky;
    public bool IsDemolishing => isDemolishing;

    private void Awake()
    {
        Instance = this;
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void BeginPlacement(BuildingRecipe recipe)
    {
        if (recipe == null || recipe.buildingPrefab == null) return;
        CancelDemolish();
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

    public void BeginDemolish() { BeginDemolish(false); }
    public void BeginDemolish(bool sticky)
    {
        CancelPlacement();
        isDemolishing = true;
        demolishSticky = sticky;
        if (selectionManager != null) selectionManager.Deselect();
    }

    public void CancelDemolish() { isDemolishing = false; demolishSticky = false; }

    private void TryDemolishAtMouse()
    {
        Vector3 mouse = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = placementZ;
        Collider2D hit = Physics2D.OverlapPoint(mouse, demolishableLayers);
        if (hit == null) return;
        Building b = hit.GetComponentInParent<Building>();
        if (b == null) return;
        Vector3 pos = b.transform.position;
        if (demolishSfxClips != null && demolishSfxClips.Length > 0) {
            var clip = demolishSfxClips[Random.Range(0, demolishSfxClips.Length)];
            if (clip != null) AudioSource.PlayClipAtPoint(clip, pos, demolishSfxVolume);
        }
        // Sacar garrisoned antes de destruir, para no perderlas.
        var occ = new System.Collections.Generic.List<Game.Units.PlayerUnit>(b.Occupants);
        foreach (var u in occ) if (u != null) b.Exit(u);
        Destroy(b.gameObject);
        if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
        if (!demolishSticky) CancelDemolish();
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
        if (isDemolishing)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !InputArbiter.EscapeConsumed)
            {
                InputArbiter.EscapeConsumed = true;
                CancelDemolish();
                return;
            }
            if (Input.GetMouseButtonDown(1) && !IsPointerOverUI()) { CancelDemolish(); return; }
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI()) { TryDemolishAtMouse(); return; }
            return;
        }
        if (!IsPlacing) return;

        if (Input.GetKeyDown(KeyCode.Escape) && !InputArbiter.EscapeConsumed)
        {
            InputArbiter.EscapeConsumed = true;
            CancelPlacement();
            return;
        }
        if (Input.GetMouseButtonDown(1) && !IsPointerOverUI())
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
        GameObject built = Instantiate(activeRecipe.buildingPrefab, pos, Quaternion.identity);
        if (buildSfxClip != null) AudioSource.PlayClipAtPoint(buildSfxClip, pos, buildSfxVolume);
        Building builtBuilding = built.GetComponent<Building>();
        if (builtBuilding != null) builtBuilding.SetOwnerRecipe(activeRecipe);

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