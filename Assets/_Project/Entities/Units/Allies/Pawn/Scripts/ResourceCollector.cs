using UnityEngine;
using Game.Core;
using Game.Managers;

namespace Game.Targeting
{

public class ResourceCollector : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (InventoryManager.Instance == null) return;

        GameObject obj = collision.gameObject;

        if (obj.CompareTag("MoneyBag"))
        {
            if (InventoryManager.Instance.TryAddMoney()) Destroy(obj);
        }
        else if (obj.CompareTag("Meat"))
        {
            if (InventoryManager.Instance.TryAddMeat()) Destroy(obj);
        }
        else if (obj.CompareTag("Wood"))
        {
            if (InventoryManager.Instance.TryAddWood()) Destroy(obj);
        }
    }
}
}
