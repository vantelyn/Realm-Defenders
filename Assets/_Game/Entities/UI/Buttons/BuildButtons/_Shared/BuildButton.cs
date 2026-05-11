using UnityEngine;
using UnityEngine.UI;
using Game.Buildings;
using Game.Managers;

namespace Game.UI
{

/// <summary>
/// Conecta un bot�n de UI con una receta concreta.
/// Al hacer clic entra en modo construcci�n.
/// </summary>
[RequireComponent(typeof(Button))]
public class BuildButton : MonoBehaviour
{
    [SerializeField] private BuildingManager placer;
    [SerializeField] private BuildingRecipe recipe;

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

    private void OnClicked()
    {
        if (placer != null && recipe != null)
        {
            placer.BeginPlacement(recipe);
        }
    }
}
}
