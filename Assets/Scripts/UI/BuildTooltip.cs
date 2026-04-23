using TMPro;
using UnityEngine;

/// <summary>
/// Popup que muestra información de un BuildingRecipe. Único en la escena, se
/// muestra y oculta bajo demanda.
/// </summary>
public class BuildTooltip : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private TMP_Text costsLabel;

    [Header("Follow")]
    [Tooltip("Offset del popup respecto al ratón (en píxeles de pantalla).")]
    [SerializeField] private Vector2 mouseOffset = new Vector2(16, -16);

    [Tooltip("Canvas al que pertenece. Se usa para convertir coords de ratón a posición del RectTransform.")]
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

    public void Show(BuildingRecipe recipe)
    {
        if (recipe == null) { Hide(); return; }

        if (titleLabel != null) titleLabel.text = recipe.displayName;
        if (descriptionLabel != null) descriptionLabel.text = recipe.description;
        if (costsLabel != null) costsLabel.text = BuildCostLine(recipe);

        root.gameObject.SetActive(true);
        visible = true;
        FollowMouse();
    }

    public void Hide()
    {
        root.gameObject.SetActive(false);
        visible = false;
    }

    private void FollowMouse()
    {
        if (parentCanvas == null) return;

        Vector2 mouseScreen = (Vector2)Input.mousePosition + mouseOffset;

        // Conversión de pantalla a coordenadas locales del canvas.
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreen, cam, out Vector2 local))
        {
            root.localPosition = local;
        }
    }

    private static string BuildCostLine(BuildingRecipe recipe)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (recipe.woodCost > 0) sb.Append($"Wood {recipe.woodCost}  ");
        if (recipe.meatCost > 0) sb.Append($"Meat {recipe.meatCost}  ");
        if (recipe.moneyCost > 0) sb.Append($"Gold {recipe.moneyCost}  ");
        return sb.ToString().TrimEnd();
    }
}