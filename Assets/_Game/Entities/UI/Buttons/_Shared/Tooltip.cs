using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Buildings;
using Game.Managers;

namespace Game.UI
{
    /// <summary>
    /// Popup que muestra informacion de una IRecipe. Soporta 3 modos:
    ///   - Show(recipe): pinta los costes con colores affordable/missing.
    ///   - ShowMissingRequirement(recipe, required): oculta los costes y muestra
    ///     el icono del edificio requerido + mensaje 'Requires X' en rojo.
    ///   - ShowAlreadyBuilt(recipe): oculta los costes y muestra mensaje
    ///     'Already built' (sin icono).
    /// </summary>
    public class BuildTooltip : MonoBehaviour
    {
        [System.Serializable]
        public class CostEntry
        {
            public GameObject root;
            public TMP_Text amountLabel;
        }

        [System.Serializable]
        public class RequirementEntry
        {
            public GameObject root;
            public Image iconImage;
            public TMP_Text messageLabel;
        }

        private enum Mode { Normal, MissingRequirement, AlreadyBuilt }

        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;

        [Header("Cost entries")]
        [SerializeField] private CostEntry woodEntry;
        [SerializeField] private CostEntry meatEntry;
        [SerializeField] private CostEntry goldEntry;

        [Header("Requirement entry")]
        [Tooltip("Slot que sustituye a los costes cuando hay un requisito sin cumplir o el edificio ya esta construido.")]
        [SerializeField] private RequirementEntry requirementEntry;

        [Header("Affordability colors")]
        [SerializeField] private Color affordColor = Color.white;
        [SerializeField] private Color missingColor = new Color(0.92f, 0.22f, 0.22f, 1f);
        [SerializeField] private Color alreadyBuiltColor = new Color(0.65f, 0.65f, 0.65f, 1f);

        [Header("Follow")]
        [SerializeField] private Vector2 mouseOffset = new Vector2(16, -16);
        [SerializeField] private Canvas parentCanvas;

        private bool visible;
        private Mode currentMode = Mode.Normal;
        private IRecipe currentRecipe;
        private BuildingRecipe currentRequiredBuilding;

        private void Awake()
        {
            if (root == null) root = transform as RectTransform;
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            Hide();
        }

        private void Update()
        {
            if (!visible) return;
            FollowMouse();
            if (currentMode == Mode.Normal) RefreshCosts();
        }

        public void Show(IRecipe recipe)
        {
            if (recipe == null) { Hide(); return; }
            currentRecipe = recipe;
            currentRequiredBuilding = null;
            currentMode = Mode.Normal;
            ApplyHeader();
            ApplyMode();
            Reveal();
        }

        public void ShowMissingRequirement(IRecipe recipe, BuildingRecipe required)
        {
            if (recipe == null) { Hide(); return; }
            currentRecipe = recipe;
            currentRequiredBuilding = required;
            currentMode = Mode.MissingRequirement;
            ApplyHeader();
            ApplyMode();
            Reveal();
        }

        public void ShowAlreadyBuilt(IRecipe recipe)
        {
            if (recipe == null) { Hide(); return; }
            currentRecipe = recipe;
            currentRequiredBuilding = null;
            currentMode = Mode.AlreadyBuilt;
            ApplyHeader();
            ApplyMode();
            Reveal();
        }

        /// <summary>Modo libre: titulo + descripcion, sin costes ni requirement. Para acciones UI.
        /// que no son recetas (ej. boton demoler).</summary>
        public void ShowText(string title, string description)
        {
            currentRecipe = null;
            currentRequiredBuilding = null;
            currentMode = Mode.Normal;
            if (titleLabel != null) titleLabel.text = title ?? "";
            if (descriptionLabel != null) descriptionLabel.text = description ?? "";
            // Ocultar todos los slots de coste y requirement.
            if (woodEntry != null && woodEntry.root != null) woodEntry.root.SetActive(false);
            if (meatEntry != null && meatEntry.root != null) meatEntry.root.SetActive(false);
            if (goldEntry != null && goldEntry.root != null) goldEntry.root.SetActive(false);
            if (requirementEntry != null && requirementEntry.root != null) requirementEntry.root.SetActive(false);
            Reveal();
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
            visible = false;
            currentRecipe = null;
            currentRequiredBuilding = null;
        }

        private void Reveal()
        {
            root.gameObject.SetActive(true);
            visible = true;
            FollowMouse();
        }

        private void ApplyHeader()
        {
            if (titleLabel != null) titleLabel.text = currentRecipe.DisplayName;
            if (descriptionLabel != null) descriptionLabel.text = currentRecipe.Description;
        }

        private void ApplyMode()
        {
            bool normal = currentMode == Mode.Normal;
            SetCostsActive(normal);
            if (requirementEntry != null && requirementEntry.root != null)
            {
                bool reqActive = !normal;
                requirementEntry.root.SetActive(reqActive);
                if (reqActive) ApplyRequirementVisuals();
            }
            if (normal) RefreshCosts();
        }

        private void ApplyRequirementVisuals()
        {
            if (requirementEntry == null) return;
            if (currentMode == Mode.MissingRequirement)
            {
                Sprite ic = ResolveBuildingIcon(currentRequiredBuilding);
                SetIcon(ic, true);
                string name = currentRequiredBuilding != null ? currentRequiredBuilding.DisplayName : "building";
                SetMessage("Requires " + name, missingColor);
            }
            else if (currentMode == Mode.AlreadyBuilt)
            {
                SetIcon(null, false);
                SetMessage("Already built", alreadyBuiltColor);
            }
        }

        private void SetIcon(Sprite s, bool show)
        {
            if (requirementEntry.iconImage == null) return;
            requirementEntry.iconImage.sprite = s;
            requirementEntry.iconImage.enabled = show && s != null;
        }

        private void SetMessage(string msg, Color color)
        {
            if (requirementEntry.messageLabel == null) return;
            requirementEntry.messageLabel.text = msg;
            requirementEntry.messageLabel.color = color;
        }

        private static Sprite ResolveBuildingIcon(BuildingRecipe r)
        {
            if (r == null) return null;
            if (r.icon != null) return r.icon;
            // Fallback: sprite del SpriteRenderer del buildingPrefab
            if (r.buildingPrefab == null) return null;
            SpriteRenderer sr = r.buildingPrefab.GetComponentInChildren<SpriteRenderer>(true);
            return sr != null ? sr.sprite : null;
        }

        private void SetCostsActive(bool active)
        {
            if (woodEntry != null && woodEntry.root != null) woodEntry.root.SetActive(active && currentRecipe.WoodCost > 0);
            if (meatEntry != null && meatEntry.root != null) meatEntry.root.SetActive(active && currentRecipe.MeatCost > 0);
            if (goldEntry != null && goldEntry.root != null) goldEntry.root.SetActive(active && currentRecipe.MoneyCost > 0);
        }

        private void RefreshCosts()
        {
            if (currentRecipe == null) return;
            var inv = InventoryManager.Instance;
            int currentWood = inv != null ? inv.Wood : int.MaxValue;
            int currentMeat = inv != null ? inv.Meat : int.MaxValue;
            int currentMoney = inv != null ? inv.Money : int.MaxValue;
            SetCost(woodEntry, currentRecipe.WoodCost, currentWood);
            SetCost(meatEntry, currentRecipe.MeatCost, currentMeat);
            SetCost(goldEntry, currentRecipe.MoneyCost, currentMoney);
        }

        private void SetCost(CostEntry entry, int amount, int current)
        {
            if (entry == null || entry.root == null) return;
            bool show = amount > 0;
            entry.root.SetActive(show);
            if (show && entry.amountLabel != null)
            {
                entry.amountLabel.text = amount.ToString();
                entry.amountLabel.color = current < amount ? missingColor : affordColor;
            }
        }

        private void FollowMouse()
        {
            if (parentCanvas == null) return;
            Vector2 mouseScreen = (Vector2)Input.mousePosition + mouseOffset;
            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreen, cam, out Vector2 local))
                return;
            Vector2 canvasHalf = canvasRect.rect.size * 0.5f;
            Vector2 size = root.rect.size;
            Vector2 pivot = root.pivot;
            local.x = Mathf.Clamp(local.x, -canvasHalf.x + size.x * pivot.x, canvasHalf.x - size.x * (1f - pivot.x));
            local.y = Mathf.Clamp(local.y, -canvasHalf.y + size.y * pivot.y, canvasHalf.y - size.y * (1f - pivot.y));
            root.localPosition = local;
        }
    }
}
