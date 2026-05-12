using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Units;
using Game.Managers;

namespace Game.UI
{

/// <summary>
/// Panel que muestra una card por cada unidad de la seleccion actual (bottom-center, horizontal).
/// Construye su jerarquia internamente: no necesita prefabs ni refs externas mas alla del
/// SelectionManager. Se suscribe al evento OnSelectionChanged para reconstruir la fila.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UnitCardsPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;

    [Header("Layout")]
    [SerializeField] private Vector2 cardSize = new Vector2(64f, 80f);
    [SerializeField] private float spacing = 4f;
    [Tooltip("Distancia desde el borde inferior del Canvas, en pixeles de referencia.")]
    [SerializeField] private float bottomMargin = 24f;

    [Header("Style")]
    [SerializeField] private Color cardBackground = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color hpBarBackground = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private int hpTextSize = 12;

    [Header("Internal Layout")]
    [SerializeField] private float cardPadding = 4f;
    [SerializeField] private float hpBarHeight = 12f;
    [SerializeField] private float hpBarMarginBottom = 2f;

    private RectTransform cardsContainer;
    private readonly List<UnitCardView> cardPool = new List<UnitCardView>();
    private Sprite whiteSprite;

    private void Awake()
    {
        if (selectionManager == null) selectionManager = FindAnyObjectByType<SelectionManager>();
        BuildContainer();
    }

    private void OnEnable()
    {
        if (selectionManager != null) selectionManager.OnSelectionChanged += Rebuild;
        Rebuild();
    }

    private void OnDisable()
    {
        if (selectionManager != null) selectionManager.OnSelectionChanged -= Rebuild;
    }

    // Re-aplica la geometria del container y destruye el pool cuando se cambian valores
    // en el inspector (en edit o en play). El siguiente Rebuild() vuelve a crear las cards
    // con los nuevos cardSize/padding/etc.
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (!isActiveAndEnabled) return;
        StartCoroutine(DeferredOnValidate());
    }

    private System.Collections.IEnumerator DeferredOnValidate()
    {
        yield return null;
        if (this == null) yield break;
        BuildContainer();
        ClearPool();
        Rebuild();
    }

    private void ClearPool()
    {
        for (int i = 0; i < cardPool.Count; i++)
        {
            if (cardPool[i] != null) Destroy(cardPool[i].gameObject);
        }
        cardPool.Clear();
    }

    private void BuildContainer()
    {
        // Este propio GameObject actua como el container, anclado bottom-center.
        RectTransform rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottomMargin);
        rt.sizeDelta = new Vector2(0f, cardSize.y); // ancho lo controla el HorizontalLayoutGroup

        HorizontalLayoutGroup hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.LowerCenter;
        hlg.spacing = spacing;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        ContentSizeFitter csf = GetComponent<ContentSizeFitter>();
        if (csf == null) csf = gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        cardsContainer = rt;

        // Sprite blanco 1x1 para fondos y barras.
        whiteSprite = CreateWhiteSprite();
    }

    private static Sprite CreateWhiteSprite()
    {
        Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[4];
        for (int i = 0; i < 4; i++) pixels[i] = Color.white;
        t.SetPixels(pixels);
        t.Apply();
        t.filterMode = FilterMode.Point;
        return Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
    }

    private UnitCardView CreateCard()
    {
        GameObject root = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        root.transform.SetParent(cardsContainer, false);

        Image bg = root.GetComponent<Image>();
        bg.sprite = whiteSprite;
        bg.color = cardBackground;
        bg.raycastTarget = true;

        LayoutElement le = root.GetComponent<LayoutElement>();
        le.preferredWidth = cardSize.x;
        le.preferredHeight = cardSize.y;

        // CRITICAL: RectTransform recien creado tiene sizeDelta default (100,100).
        // El HLG con childControlWidth=false NO redimensiona el child, asi que sin esto
        // las cards aparecen siempre a 100x100 independientemente del cardSize configurado.
        RectTransform rootRT = (RectTransform)root.transform;
        rootRT.sizeDelta = cardSize;

        Button btn = root.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;

        // Espacio reservado abajo para la barra + margen.
        float reservedBottom = hpBarMarginBottom + hpBarHeight + cardPadding;

        // Portrait
        GameObject portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        portraitGO.transform.SetParent(root.transform, false);
        RectTransform pRT = (RectTransform)portraitGO.transform;
        pRT.anchorMin = new Vector2(0f, 0f);
        pRT.anchorMax = new Vector2(1f, 1f);
        pRT.offsetMin = new Vector2(cardPadding, reservedBottom);
        pRT.offsetMax = new Vector2(-cardPadding, -cardPadding);
        Image portraitImg = portraitGO.GetComponent<Image>();
        portraitImg.preserveAspect = true;
        portraitImg.raycastTarget = false;

        // HpBar background (anclada al fondo)
        GameObject hpBgGO = new GameObject("HpBarBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hpBgGO.transform.SetParent(root.transform, false);
        RectTransform hpBgRT = (RectTransform)hpBgGO.transform;
        hpBgRT.anchorMin = new Vector2(0f, 0f);
        hpBgRT.anchorMax = new Vector2(1f, 0f);
        hpBgRT.pivot = new Vector2(0.5f, 0f);
        hpBgRT.offsetMin = new Vector2(cardPadding, hpBarMarginBottom);
        hpBgRT.offsetMax = new Vector2(-cardPadding, hpBarMarginBottom + hpBarHeight);
        Image hpBg = hpBgGO.GetComponent<Image>();
        hpBg.sprite = whiteSprite;
        hpBg.color = hpBarBackground;
        hpBg.raycastTarget = false;

        // HpFill (filled horizontal, dentro del bg con margen 1px)
        GameObject hpFillGO = new GameObject("HpFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hpFillGO.transform.SetParent(hpBgGO.transform, false);
        RectTransform hpFillRT = (RectTransform)hpFillGO.transform;
        hpFillRT.anchorMin = new Vector2(0f, 0f);
        hpFillRT.anchorMax = new Vector2(1f, 1f);
        hpFillRT.offsetMin = new Vector2(1f, 1f);
        hpFillRT.offsetMax = new Vector2(-1f, -1f);
        Image hpFill = hpFillGO.GetComponent<Image>();
        hpFill.sprite = whiteSprite;
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hpFill.fillAmount = 1f;
        hpFill.color = new Color(0.30f, 0.85f, 0.30f);
        hpFill.raycastTarget = false;

        // HpText (centrado sobre la barra)
        GameObject hpTxtGO = new GameObject("HpText", typeof(RectTransform), typeof(CanvasRenderer));
        hpTxtGO.transform.SetParent(hpBgGO.transform, false);
        RectTransform tRT = (RectTransform)hpTxtGO.transform;
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = hpTxtGO.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = "";
        tmp.fontSize = hpTextSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;

        UnitCardView view = root.AddComponent<UnitCardView>();
        view.Init(portraitImg, hpFill, tmp, btn);
        return view;
    }

    public void Rebuild()
    {
        IReadOnlyList<PlayerUnit> sel = selectionManager != null ? selectionManager.SelectedUnits : null;
        int count = sel != null ? sel.Count : 0;

        // Crecer el pool
        while (cardPool.Count < count)
        {
            cardPool.Add(CreateCard());
        }

        // Bind / hide
        for (int i = 0; i < cardPool.Count; i++)
        {
            UnitCardView card = cardPool[i];
            if (i < count)
            {
                if (!card.gameObject.activeSelf) card.gameObject.SetActive(true);
                card.Bind(sel[i], selectionManager);
            }
            else
            {
                if (card.gameObject.activeSelf) card.gameObject.SetActive(false);
            }
        }
    }
}

}
