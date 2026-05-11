using UnityEngine;
using UnityEngine.UI;
using Game.Buildings;

namespace Game.UI
{

[RequireComponent(typeof(Button))]
public class SpawnButton : MonoBehaviour
{
    [SerializeField] private UnitSpawner spawner;
    [SerializeField] private UnitRecipe recipe;

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
        if (spawner != null && recipe != null) spawner.TrySpawn(recipe);
    }
}
}
