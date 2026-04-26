using TMPro;
using UnityEngine;

namespace Game.UI
{

/// <summary>
/// Popup que muestra informaci�n de una IRecipe (building o unit). �nico en la
/// escena, se muestra y oculta bajo demanda.
/// </summary>
public class BuildTooltip : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private TMP_Text costsLabel;

    [Header("Follow")]
    [Tooltip("Offset del popup respecto al rat�n (en p�xeles de pantalla).")]
    [SerializeField] private Vector2 mouseOffset = new Vector2(16, -16);

    [Tooltip("Canvas al que pertenece. Se usa para convertir coords de rat�n a posici�n del RectTransform.")]
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

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreen, cam, out Vector2 local))
        {
            root.localPosition = local;
        }
    }

    private static string BuildCostLine(IRecipe recipe)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (recipe.WoodCost > 0) sb.Append($"Wood {recipe.WoodCost}  ");
        if (recipe.MeatCost > 0) sb.Append($"Meat {recipe.MeatCost}  ");
        if (recipe.MoneyCost > 0) sb.Append($"Gold {recipe.MoneyCost}  ");
        return sb.ToString().TrimEnd();
    }
}
}
