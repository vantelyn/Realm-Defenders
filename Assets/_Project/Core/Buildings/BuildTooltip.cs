using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Popup que muestra informacion de una IRecipe (building o unit). Unico en la
    /// escena, se muestra y oculta bajo demanda. Usa iconos para los costes.
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

        [Header("Follow")]
        [Tooltip("Offset del popup respecto al raton (en pixeles de pantalla).")]
        [SerializeField] private Vector2 mouseOffset = new Vector2(16, -16);

        [Tooltip("Canvas al que pertenece.")]
        [SerializeField] private Canvas parentCanvas;

        private bool visible;

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
        }

        public void Show(IRecipe recipe)
        {
            if (recipe == null) { Hide(); return; }

            if (titleLabel != null) titleLabel.text = recipe.DisplayName;
            if (descriptionLabel != null) descriptionLabel.text = recipe.Description;

            SetCost(woodEntry, recipe.WoodCost);
            SetCost(meatEntry, recipe.MeatCost);
            SetCost(goldEntry, recipe.MoneyCost);

            root.gameObject.SetActive(true);
            visible = true;
            FollowMouse();
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
            visible = false;
        }

        private static void SetCost(CostEntry entry, int amount)
        {
            if (entry == null || entry.root == null) return;
            bool show = amount > 0;
            entry.root.SetActive(show);
            if (show && entry.amountLabel != null)
                entry.amountLabel.text = amount.ToString();
        }

        private void FollowMouse()
        {
            if (parentCanvas == null) return;

            Vector2 mouseScreen = (Vector2)Input.mousePosition + mouseOffset;
            RectTransform canvasRect = parentCanvas.transform as RectTransform;
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreen, cam, out Vector2 local))
                return;

            // Clamp to canvas bounds  tooltip never clips off screen
            Vector2 canvasHalf = canvasRect.rect.size * 0.5f;
            Vector2 size = root.rect.size;
            Vector2 pivot = root.pivot;

            local.x = Mathf.Clamp(local.x, -canvasHalf.x + size.x * pivot.x, canvasHalf.x - size.x * (1f - pivot.x));
            local.y = Mathf.Clamp(local.y, -canvasHalf.y + size.y * pivot.y, canvasHalf.y - size.y * (1f - pivot.y));

            root.localPosition = local;
        }
    }
}
