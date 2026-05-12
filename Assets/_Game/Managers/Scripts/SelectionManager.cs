using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Buildings;
using Game.Config;
using Game.Combat;
using Game.Units;

namespace Game.Managers
{

public class SelectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private TargetingConfig targeting;
    [SerializeField] private BuildingManager buildPlacer;

    [Header("Selection Box")]
    [Tooltip("Pixeles que el raton tiene que arrastrar para entrar en modo box-select. Por debajo se considera click corto.")]
    [SerializeField] private float dragThresholdPixels = 6f;
    [SerializeField] private Color boxFillColor = new Color(0.3f, 0.7f, 1f, 0.18f);

    private readonly List<PlayerUnit> selectedUnits = new List<PlayerUnit>();
    private static readonly List<PlayerUnit> reusableHits = new List<PlayerUnit>();

    // Drag state
    private bool mouseHeld;
    private bool isDragging;
    private Vector2 mouseDownScreen;

    // Box visual (creado en runtime bajo el Canvas principal)
    private RectTransform boxRT;
    private bool triedCreateBox;

    // ---- API publica ----

    /// <summary>Primera unidad de la seleccion (alias para codigo legacy).</summary>
    public PlayerUnit SelectedUnit => selectedUnits.Count > 0 ? selectedUnits[0] : null;
    public IReadOnlyList<PlayerUnit> SelectedUnits => selectedUnits;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Update()
    {
        // Esc deselecciona todo (modal stack via InputArbiter).
        if (Input.GetKeyDown(KeyCode.Escape) && !InputArbiter.EscapeConsumed && selectedUnits.Count > 0)
        {
            InputArbiter.EscapeConsumed = true;
            DeselectAll();
            return;
        }

        // F tambien deselecciona (alias rapido al alcance del WASD).
        if (Input.GetKeyDown(KeyCode.F) && selectedUnits.Count > 0)
        {
            DeselectAll();
            return;
        }

        HandleLeftMouse();

        // RMB: secondary action (Warrior block, etc.) sobre la primera unidad de la seleccion.
        // Sprint B reasignara esto a movimiento contextual y movera secondary a Q.
        if (Input.GetMouseButtonDown(1)) HandleSecondaryAction();
        if (Input.GetMouseButtonUp(1)) HandleSecondaryRelease();
    }

    // ---- LMB: click + box drag ----

    private void HandleLeftMouse()
    {
        if (buildPlacer != null && buildPlacer.IsPlacing) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI())
            {
                mouseHeld = false;
                return;
            }
            mouseDownScreen = Input.mousePosition;
            mouseHeld = true;
            isDragging = false;
        }

        if (!mouseHeld) return;

        if (Input.GetMouseButton(0))
        {
            Vector2 nowScreen = Input.mousePosition;
            if (!isDragging && Vector2.Distance(nowScreen, mouseDownScreen) >= dragThresholdPixels)
            {
                isDragging = true;
                ShowBoxVisual(true);
            }
            if (isDragging) UpdateBoxVisual(mouseDownScreen, nowScreen);
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 endScreen = Input.mousePosition;
            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (isDragging)
            {
                DoBoxSelect(mouseDownScreen, endScreen, additive);
            }
            else
            {
                ProcessSingleClick(endScreen, additive);
            }

            mouseHeld = false;
            isDragging = false;
            ShowBoxVisual(false);
        }
    }

    private void ProcessSingleClick(Vector2 screenPoint, bool additive)
    {
        if (targeting == null) return;
        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(screenPoint);
        mouseWorld.z = 0f;

        // 1. Edificio bajo el cursor?
        Building building = QueryService.FindBuildingAt(mouseWorld, targeting.buildingsLayer, targeting.buildingTag);
        if (building != null)
        {
            HandleBuildingClick(building, mouseWorld);
            return;
        }

        // 2. Unidad bajo el cursor?
        PlayerUnit hit = QueryService.FindUnitAt(mouseWorld, targeting.unitsLayer, targeting.unitTag);
        if (hit != null)
        {
            if (additive) ToggleInSelection(hit);
            else SelectSingle(hit);
            return;
        }

        // 3. Click corto en vacio: si hay seleccion, cada unidad ataca hacia el cursor.
        // (Si fue drag, el flujo va por DoBoxSelect, no por aqui.)
        if (selectedUnits.Count == 0) return;
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            PlayerUnit u = selectedUnits[i];
            if (u == null) continue;
            Vector2 aim = (Vector2)(mouseWorld - u.transform.position);
            u.PrimaryAttack(aim);
        }
    }

    private void HandleBuildingClick(Building building, Vector3 mouseWorld)
    {
        // Garrison masivo todavia no soportado. Solo actuamos con 1 unidad seleccionada.
        if (selectedUnits.Count == 1)
        {
            PlayerUnit u = selectedUnits[0];
            if (u.IsGarrisoned && u.CurrentBuilding == building)
            {
                building.Exit(u);
                return;
            }
            if (!u.IsGarrisoned && building.HasFreeSlot)
            {
                building.TryEnter(u);
                return;
            }
            return;
        }

        // Sin seleccion: si hay una unidad garrisoned dentro, seleccionarla.
        if (selectedUnits.Count == 0)
        {
            PlayerUnit garrisoned = QueryService.FindUnitAt(mouseWorld, targeting.unitsLayer, targeting.unitTag);
            if (garrisoned != null && garrisoned.IsGarrisoned && garrisoned.CurrentBuilding == building)
            {
                SelectSingle(garrisoned);
            }
        }
        // >1 seleccionadas: ignoramos el click sobre edificio (limitacion temporal).
    }

    private void DoBoxSelect(Vector2 screenStart, Vector2 screenEnd, bool additive)
    {
        if (targeting == null) return;
        Vector3 wa = worldCamera.ScreenToWorldPoint(screenStart);
        Vector3 wb = worldCamera.ScreenToWorldPoint(screenEnd);
        Vector2 worldMin = Vector2.Min((Vector2)wa, (Vector2)wb);
        Vector2 worldMax = Vector2.Max((Vector2)wa, (Vector2)wb);

        QueryService.FindUnitsInBox(worldMin, worldMax, targeting.unitsLayer, targeting.unitTag, reusableHits);

        if (!additive) DeselectAll();
        for (int i = 0; i < reusableHits.Count; i++)
        {
            PlayerUnit u = reusableHits[i];
            if (u == null) continue;
            if (selectedUnits.Contains(u)) continue;
            AddToSelection(u);
        }
        UpdateCameraFollow();
    }

    // ---- Mutadores de seleccion ----

    private void AddToSelection(PlayerUnit u)
    {
        if (u == null) return;
        if (selectedUnits.Contains(u)) return;
        selectedUnits.Add(u);
        u.SetSelected(true);
    }

    private void RemoveFromSelection(PlayerUnit u)
    {
        if (u == null) return;
        if (!selectedUnits.Remove(u)) return;
        u.SetSelected(false);
    }

    public void DeselectAll()
    {
        for (int i = 0; i < selectedUnits.Count; i++)
            if (selectedUnits[i] != null) selectedUnits[i].SetSelected(false);
        selectedUnits.Clear();
        UpdateCameraFollow();
    }

    /// <summary>Alias retrocompatible.</summary>
    public void Deselect() => DeselectAll();

    private void SelectSingle(PlayerUnit u)
    {
        DeselectAll();
        AddToSelection(u);
        UpdateCameraFollow();
    }

    private void ToggleInSelection(PlayerUnit u)
    {
        if (selectedUnits.Contains(u)) RemoveFromSelection(u);
        else AddToSelection(u);
        UpdateCameraFollow();
    }

    private void UpdateCameraFollow()
    {
        Transform follow = (selectedUnits.Count > 0 && selectedUnits[0] != null) ? selectedUnits[0].transform : null;
        CameraManager.SetFollowTarget(follow);
    }

    // ---- RMB: secondary action (sin cambios respecto al flujo previo) ----

    private void HandleSecondaryAction()
    {
        if (buildPlacer != null && buildPlacer.IsPlacing) return;
        if (IsPointerOverUI()) return;
        if (selectedUnits.Count == 0) return;
        if (targeting == null) return;

        PlayerUnit primary = selectedUnits[0];
        if (primary == null || !primary.HasSecondary) return;

        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        PlayerUnit hovered = QueryService.FindUnitAt(mouseWorld, targeting.unitsLayer, targeting.unitTag);

        if (hovered != null && hovered != primary && !primary.SecondaryTargetsAllies) return;

        Vector2 aim = (Vector2)(mouseWorld - primary.transform.position);
        primary.SecondaryAction(aim, hovered);
    }

    private void HandleSecondaryRelease()
    {
        if (selectedUnits.Count == 0) return;
        PlayerUnit primary = selectedUnits[0];
        if (primary != null) primary.EndSecondaryAction();
    }

    // ---- Box visual ----

    private void EnsureBoxVisual()
    {
        if (boxRT != null) return;
        if (triedCreateBox) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) { triedCreateBox = true; return; }

        GameObject root = new GameObject("SelectionBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boxRT = (RectTransform)root.transform;
        boxRT.SetParent(canvas.transform, false);
        boxRT.SetAsLastSibling();

        // Anchor centrado para que coincida con el sistema de coordenadas que devuelve
        // ScreenPointToLocalPointInRectangle (relativo al pivot del canvas, normalmente 0.5,0.5).
        // Pivot en (0,0) para que sizeDelta crezca hacia arriba-derecha desde anchoredPosition.
        boxRT.anchorMin = boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0f, 0f);

        Image fill = root.GetComponent<Image>();
        fill.color = boxFillColor;
        fill.raycastTarget = false;

        root.SetActive(false);
        triedCreateBox = true;
    }

    private void ShowBoxVisual(bool show)
    {
        EnsureBoxVisual();
        if (boxRT == null) return;
        boxRT.gameObject.SetActive(show);
        if (show) boxRT.SetAsLastSibling();
    }

    private void UpdateBoxVisual(Vector2 screenStart, Vector2 screenCurrent)
    {
        if (boxRT == null) return;
        RectTransform canvasRT = boxRT.parent as RectTransform;
        if (canvasRT == null) return;
        Vector2 localStart, localCurrent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenStart, null, out localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenCurrent, null, out localCurrent);
        Vector2 min = Vector2.Min(localStart, localCurrent);
        Vector2 max = Vector2.Max(localStart, localCurrent);
        boxRT.anchoredPosition = min;
        boxRT.sizeDelta = max - min;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
}
