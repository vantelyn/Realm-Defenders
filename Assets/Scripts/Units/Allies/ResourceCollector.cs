using UnityEngine;

public class ResourceCollector : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private int money = 0;
    private int meat = 0;
    private int wood = 0;
    private int maxMoney = 10;
    private int maxMeat = 10;
    private int maxWood = 10;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("MoneyBag") && money < maxMoney)
        {
            Destroy(collision.gameObject);
            money++;
        }
        if (collision.gameObject.CompareTag("Meat") && meat < maxMeat)
        {
            Destroy(collision.gameObject);
            meat++;
        }
        if (collision.gameObject.CompareTag("Wood") && wood < maxWood)
        {
            Destroy(collision.gameObject);
            wood++;
        }
    }
}
