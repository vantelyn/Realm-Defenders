using TMPro;
using UnityEngine;
using Game.Managers;

namespace Game.UI
{

/// <summary>
/// Muestra un recurso del InventoryManager en un TextMeshPro.
/// Reactivo: se actualiza al cambiar el inventario. Robusto al orden de
/// inicializacion: si el InventoryManager aun no existe en OnEnable,
/// reintenta la suscripcion cada frame en Update hasta conseguirla.
/// </summary>
public class ResourceCounter : MonoBehaviour
{
    public enum ResourceKind { Money, Meat, Wood }

    [SerializeField] private ResourceKind resource;
    [Tooltip("Si true, muestra 'actual/max' en vez de solo 'actual'.")]
    [SerializeField] private bool showMax = false;

    [SerializeField] private TMP_Text label;

    private bool subscribed = false;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (InventoryManager.Instance == null) return;
        InventoryManager.Instance.OnChanged += Refresh;
        subscribed = true;
        Refresh();
    }

    private void OnDisable()
    {
        if (subscribed && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnChanged -= Refresh;
        }
        subscribed = false;
    }

    private void Refresh()
    {
        if (label == null || InventoryManager.Instance == null) return;

        int current;
        int max;
        switch (resource)
        {
            case ResourceKind.Money: current = InventoryManager.Instance.Money; max = InventoryManager.Instance.MaxMoney; break;
            case ResourceKind.Meat: current = InventoryManager.Instance.Meat; max = InventoryManager.Instance.MaxMeat; break;
            case ResourceKind.Wood: current = InventoryManager.Instance.Wood; max = InventoryManager.Instance.MaxWood; break;
            default: current = 0; max = 0; break;
        }

        label.text = showMax ? $"{current}/{max}" : current.ToString();
    }
}
}
