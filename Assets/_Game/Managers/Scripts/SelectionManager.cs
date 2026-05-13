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

    // ---- Eventos ----
    public event System.Action OnSelectionChanged;
    private bool selectionDirty;


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
        // Evento diferido: colapsa multiples mutaciones del mismo frame en un unico raise.
        if (selectionDirty)
        {
            selectionDirty = false;
            if (OnSelectionChanged != null) OnSelectionChanged();
        }

        
if (Input.GetKeyDown(KeyCode.Escape) && !InputArbiter.EscapeConsumed && selectedUnits.Count > 0)
        {
            InputArbiter.EscapeConsumed = true;
            DeselectAll();
            return;
        }

        // F tambien deselecciona.
        if (Input.GetKeyDown(KeyCode.F) && selectedUnits.Count > 0)
        {
            DeselectAll();
            return;
        }

        HandleLeftMouse();
        HandleRightMouse();
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

    // ---- RMB: contextual (mover / atacar / recolectar) ----

    private void HandleRightMouse()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (buildPlacer != null && buildPlacer.IsPlacing) return;
        if (IsPointerOverUI()) return;
        if (selectedUnits.Count == 0) return;
        if (targeting == null) return;

        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        // 0. Building bajo el cursor.
        //    - Si hay alguna garrisoned en la seleccion -> sacarlas TODAS (de cualquier building).
        //    - Si no hay garrisoned -> meter las seleccionadas en este building hasta agotar slots.
        Building building = QueryService.FindBuildingAt(mouseWorld, targeting.buildingsLayer, targeting.buildingTag);
        if (building != null)
        {
            if (TryExitAnyGarrisoned()) return;
            TryEnterSelectedInto(building);
            return;
        }

        Transform enemy = FindEnemyAt(mouseWorld);
        if (enemy != null) { CommandAll_Attack(enemy); return; }
        Transform resource = FindResourceAt(mouseWorld);
        if (resource != null) { CommandAll_Harvest(resource); return; }
        CommandAll_MoveTo(mouseWorld);
    }

    private bool TryExitAnyGarrisoned()
    {
        bool any = false;
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            PlayerUnit u = selectedUnits[i];
            if (u == null) continue;
            if (!u.IsGarrisoned) continue;
            Building b = u.CurrentBuilding;
            if (b == null) continue;
            b.Exit(u);
            MarkAutoGarrison(u, false);
            any = true;
        }
        return any;
    }

    private bool TryEnterSelectedInto(Building building)
    {
        if (building == null) return false;
        bool any = false;
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            PlayerUnit u = selectedUnits[i];
            if (u == null) continue;
            if (u.IsGarrisoned) continue;          // ya esta dentro de algun building
            if (!building.HasFreeSlot) break;       // sin slots, no seguimos intentando
            if (building.TryEnter(u))
            {
                MarkAutoGarrison(u, true);
                any = true;
            }
        }
        return any;
    }

    private void MarkAutoGarrison(PlayerUnit u, bool allowAutoGarrison)
    {
        if (u == null) return;
        Game.AI.BaseUnitAI ai = u.GetComponent<Game.AI.BaseUnitAI>();
        if (ai != null) ai.NoAutoGarrison = !allowAutoGarrison;
    }

    private Transform FindEnemyAt(Vector3 worldPoint)
    {
        int enemyMask = 0;
        int li;
        li = LayerMask.NameToLayer("EnemyHitbox"); if (li >= 0) enemyMask |= (1 << li);
        li = LayerMask.NameToLayer("NeutralHitbox");  if (li >= 0) enemyMask |= (1 << li);
        if (enemyMask == 0) return null;
        Collider2D col = Physics2D.OverlapPoint(worldPoint, enemyMask);
        if (col == null) return null;
        // Devolvemos el Transform del hitbox: es el que usa la IA libre via enemyDetector,
        // para que CommandAttackTarget sea consistente con el flujo automatico.
        return col.transform;
    }

    private Transform FindResourceAt(Vector3 worldPoint)
    {
        int resourceMask = 0;
        int li;
        li = LayerMask.NameToLayer("Resources");   if (li >= 0) resourceMask |= (1 << li);
        li = LayerMask.NameToLayer("Tree");        if (li >= 0) resourceMask |= (1 << li);
        li = LayerMask.NameToLayer("Sheep");       if (li >= 0) resourceMask |= (1 << li);
        li = LayerMask.NameToLayer("SheepHitbox"); if (li >= 0) resourceMask |= (1 << li);
        if (resourceMask == 0) return null;
        Collider2D col = Physics2D.OverlapPoint(worldPoint, resourceMask);
        if (col == null) return null;
        // Si el hit es el hitbox de oveja, devolvemos el root (layer Sheep) para que
        // CommandHarvestTarget lo detecte como oveja y redirija a CommandAttackTarget.
        int sheepHitboxLayer = LayerMask.NameToLayer("SheepHitbox");
        if (sheepHitboxLayer >= 0 && col.gameObject.layer == sheepHitboxLayer)
        {
            Transform tr = col.transform;
            while (tr != null && tr.gameObject.layer == sheepHitboxLayer) tr = tr.parent;
            if (tr != null) return tr;
        }
        return col.transform;
    }

    private void CommandAll_Attack(Transform enemy)
    {
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            PlayerUnit u = selectedUnits[i];
            if (u == null) continue;
            Game.AI.BaseUnitAI ai = u.GetComponent<Game.AI.BaseUnitAI>();
            if (ai != null) ai.CommandAttackTarget(enemy);
        }
    }

    private void CommandAll_Harvest(Transform resource)
    {
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            PlayerUnit u = selectedUnits[i];
            if (u == null) continue;
            Game.AI.BaseUnitAI ai = u.GetComponent<Game.AI.BaseUnitAI>();
            if (ai != null) ai.CommandHarvestTarget(resource);
        }
    }

    private void CommandAll_MoveTo(Vector3 worldPoint)
    {
        // Formacion: rejilla cuadrada alrededor del punto. Side ~ sqrt(N).
        int n = selectedUnits.Count;
        int side = Mathf.CeilToInt(Mathf.Sqrt(n));
        const float spacing = 1.3f;
        float halfSpan = (side - 1) * spacing * 0.5f;

        int idx = 0;
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            PlayerUnit u = selectedUnits[i];
            if (u == null) continue;
            int row = idx / side;
            int col = idx % side;
            Vector3 offset = new Vector3(col * spacing - halfSpan, row * spacing - halfSpan, 0f);
            Vector3 dest = worldPoint + offset;
            Game.AI.BaseUnitAI ai = u.GetComponent<Game.AI.BaseUnitAI>();
            if (ai != null) ai.CommandMoveTo(dest);
            idx++;
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
            HandleBuildingClick(building, mouseWorld, additive);
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

        // 3. Click en vacio: nada. Los comandos van por RMB.
    }

    private void HandleBuildingClick(Building building, Vector3 mouseWorld, bool additive)
    {
        // LMB sobre building NO entra ni sale. Eso es exclusivo del RMB.
        // Aqui solo permitimos seleccionar a la unidad garrisoned dentro (esta tapada visualmente).
        PlayerUnit garrisoned = QueryService.FindUnitAt(mouseWorld, targeting.unitsLayer, targeting.unitTag);
        if (garrisoned == null || !garrisoned.IsGarrisoned || garrisoned.CurrentBuilding != building) return;

        if (additive)
        {
            // Shift-LMB sobre building: toggle la garrisoned. Permite formar grupos mixtos
            // (dentro + fuera) que el RMB sobre puerta usara para liberar.
            ToggleInSelection(garrisoned);
            return;
        }

        if (selectedUnits.Count == 0)
        {
            SelectSingle(garrisoned);
        }
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
        selectionDirty = true;
    }

    private void RemoveFromSelection(PlayerUnit u)
    {
        if (u == null) return;
        if (!selectedUnits.Remove(u)) return;
        u.SetSelected(false);
        selectionDirty = true;
        // Al deseleccionar manualmente, la IA libre recupera el control. Si tiene seekBuildings
        // y hay una torre cercana, la usara automaticamente.
        MarkAutoGarrison(u, true);
    }

    public void DeselectAll()
    {
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            PlayerUnit u = selectedUnits[i];
            if (u == null) continue;
            u.SetSelected(false);
            // Mismo reseteo que en RemoveFromSelection: dejar a la IA libre actuar.
            MarkAutoGarrison(u, true);
        }
        selectedUnits.Clear();
        selectionDirty = true;
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

    /// <summary>API publica para que UI externa (cards) seleccione una unica unidad.</summary>
    public void SelectOnly(PlayerUnit u) => SelectSingle(u);

    private void ToggleInSelection(PlayerUnit u)
    {
        if (selectedUnits.Contains(u)) RemoveFromSelection(u);
        else AddToSelection(u);
        UpdateCameraFollow();
    }

    private void UpdateCameraFollow()
    {
        // AoE-style: la camara nunca sigue. WASD siempre mueve camara libre.
        CameraManager.SetFollowTarget(null);
    }

    /// <summary>
    /// Sincroniza el solo-mode: cuando hay exactamente 1 unidad seleccionada,
    /// esa unidad pasa a Mode=Player (WASD/Q/E). Otra configuracion devuelve todas a Mode=AI.
    /// </summary>



    // ---- RMB: secondary action (sin cambios respecto al flujo previo) ----





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