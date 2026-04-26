using UnityEngine;
using Game.Core;

namespace Game.Targeting
{

public class ResourceCollector : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (inventory == null) return;

        GameObject obj = collision.gameObject;

        if (obj.CompareTag("MoneyBag"))
        {
            if (inventory.TryAddMoney()) Destroy(obj);
        }
        else if (obj.CompareTag("Meat"))
        {
            if (inventory.TryAddMeat()) Destroy(obj);
        }
        else if (obj.CompareTag("Wood"))
        {
            if (inventory.TryAddWood()) Destroy(obj);
        }
    }
}
}
