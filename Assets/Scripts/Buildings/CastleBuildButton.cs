using UnityEngine;
using UnityEngine.UI;
using Game.Core;

namespace Game.Buildings
{

/// <summary>
/// Bot�n espec�fico para el castillo. Tiene dos modos:
/// - Sin castillo construido: entra en modo colocaci�n (BuildPlacer + CastleRecipe).
/// - Con castillo existente: intenta hacer upgrade in-place.
/// Se actualiza reactivamente cuando cambian los recursos o el nivel del castillo.
/// </summary>
[RequireComponent(typeof(Button))]
public class CastleBuildButton : MonoBehaviour
{
    [SerializeField] private BuildPlacer placer;
    [SerializeField] private BuildingRecipe initialRecipe;  // la misma receta de construcci�n original.
    [SerializeField] private PlayerInventory inventory;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClicked);
    }

    private void OnEnable()
    {
        if (inventory != null) inventory.OnChanged += RefreshInteractable;
        RefreshInteractable();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= RefreshInteractable;
    }

    private void Update()
    {
        // Barato, pero cubre los cambios de nivel del castillo sin tener que exponer eventos adicionales.
        RefreshInteractable();
    }

    private void OnClicked()
    {
        if (Castle.Instance == null)
        {
            if (placer != null && initialRecipe != null) placer.BeginPlacement(initialRecipe);
            return;
        }

        Castle.Instance.TryUpgrade(inventory);
    }

    private void RefreshInteractable()
    {
        if (button == null) return;

        if (Castle.Instance == null)
        {
            button.interactable = initialRecipe != null && inventory != null && initialRecipe.CanAfford(inventory);
            return;
        }

        Castle castle = Castle.Instance;
        if (castle.IsMaxLevel || castle.IsEvolving)
        {
            button.interactable = false;
            return;
        }

        button.interactable = castle.CanAffordNextUpgrade(inventory);
    }
}
}
