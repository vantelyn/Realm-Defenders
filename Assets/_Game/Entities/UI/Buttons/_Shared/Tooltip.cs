using TMPro;
using UnityEngine;
using Game.Buildings;
using Game.Managers;

namespace Game.UI
{
    /// <summary>
    /// Popup que muestra informacion de una IRecipe (building o unit). Unico en la
    /// escena, se muestra y oculta bajo demanda. Pinta de rojo los costes que el
    /// jugador no puede pagar con su inventario actual.
    /// </summary>
    public class BuildTooltip : MonoBehaviour
    {
        [System.Serializable]
        public class CostEntry
        {
            public GameObject root;
            public TMP_Text amountLabel;
        }

        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;

        [Header("Cost entries")]
        [SerializeField] private CostEntry woodEntry;
        [SerializeField] private CostEntry meatEntry;
        [SerializeField] private CostEntry goldEntry;

        [Header("Affordability colors")]
        [Tooltip("Color del coste cuando el jugador tiene suficiente recurso.")]
        [SerializeField] private Color affordColor = Color.white;
        [Tooltip("Color del coste cuando el jugador NO tiene suficiente recurso.")]
        [SerializeField] private Color missingColor = new Color(0.92f, 0.22f, 0.22f, 1f);

        [Header("Follow")]
        [Tooltip("Offset del popup respecto al raton (en pixeles de pantalla).")]
        [SerializeField] private Vector2 mouseOffset = new Vector2(16, -16);

        [Tooltip("Canvas al que pertenece.")]
        [SerializeField] private Canvas parentCanvas;

        private bool visible;
        private IRecipe currentRecipe;

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
            // Refresca costes en cada frame mientras visible: si el inventario cambia
            // (pickup, gasto, etc.) los colores reflejan el estado actual sin esperar.
            RefreshCosts();
        }

        public void Show(IRecipe recipe)
        {
            if (recipe == null) { Hide(); return; }

            currentRecipe = recipe;
            if (titleLabel != null) titleLabel.text = recipe.DisplayName;
            if (descriptionLabel != null) descriptionLabel.text = recipe.Description;

            RefreshCosts();

            root.gameObject.SetActive(true);
            visible = true;
            FollowMouse();
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
            visible = false;
            currentRecipe = null;
        }

        private void RefreshCosts()
        {
            if (currentRecipe == null) return;
            // Si no hay InventoryManager, asumimos infinito (no pinta nada en rojo).
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

            // Clamp to canvas bounds - tooltip never clips off screen
            Vector2 canvasHalf = canvasRect.rect.size * 0.5f;
            Vector2 size = root.rect.size;
            Vector2 pivot = root.pivot;

            local.x = Mathf.Clamp(local.x, -canvasHalf.x + size.x * pivot.x, canvasHalf.x - size.x * (1f - pivot.x));
            local.y = Mathf.Clamp(local.y, -canvasHalf.y + size.y * pivot.y, canvasHalf.y - size.y * (1f - pivot.y));

            root.localPosition = local;
        }
    }
}
