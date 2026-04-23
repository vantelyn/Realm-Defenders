using TMPro;
using UnityEngine;

/// <summary>
/// Muestra un recurso del PlayerInventory en un TextMeshPro.
/// Reactivo: se actualiza automáticamente cuando el inventario cambia.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ResourceDisplay : MonoBehaviour
{
    public enum ResourceKind { Money, Meat, Wood }

    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private ResourceKind resource;
    [Tooltip("Si true, muestra 'actual/max' en vez de solo 'actual'.")]
    [SerializeField] private bool showMax = false;

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnChanged += Refresh;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (label == null || inventory == null) return;

        int current;
        int max;
        switch (resource)
        {
            case ResourceKind.Money: current = inventory.Money; max = inventory.MaxMoney; break;
            case ResourceKind.Meat: current = inventory.Meat; max = inventory.MaxMeat; break;
            case ResourceKind.Wood: current = inventory.Wood; max = inventory.MaxWood; break;
            default: current = 0; max = 0; break;
        }

        label.text = showMax ? $"{current}/{max}" : current.ToString();
    }
}