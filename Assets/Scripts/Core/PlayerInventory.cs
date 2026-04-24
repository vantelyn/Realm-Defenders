using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInventory", menuName = "Game/Player Inventory")]
public class PlayerInventory : ScriptableObject
{
    [Header("Capacity")]
    [SerializeField] private int maxMoney = 100;
    [SerializeField] private int maxMeat = 100;
    [SerializeField] private int maxWood = 100;

    [Header("Starting Amounts")]
    [SerializeField] private int startingMoney = 20;
    [SerializeField] private int startingMeat = 20;
    [SerializeField] private int startingWood = 20;

    // ...



    private int money;
    private int meat;
    private int wood;

    public int Money => money;
    public int Meat => meat;
    public int Wood => wood;

    public int MaxMoney => maxMoney;
    public int MaxMeat => maxMeat;
    public int MaxWood => maxWood;

    public event System.Action OnChanged;

    public bool TryAddMoney(int amount = 1) => TryAdd(ref money, maxMoney, amount);
    public bool TryAddMeat(int amount = 1) => TryAdd(ref meat, maxMeat, amount);
    public bool TryAddWood(int amount = 1) => TryAdd(ref wood, maxWood, amount);

    public bool TrySpendMoney(int amount) => TrySpend(ref money, amount);
    public bool TrySpendMeat(int amount) => TrySpend(ref meat, amount);
    public bool TrySpendWood(int amount) => TrySpend(ref wood, amount);

    private bool TryAdd(ref int store, int max, int amount)
    {
        if (store + amount > max) return false;
        store += amount;
        OnChanged?.Invoke();
        return true;
    }

    private bool TrySpend(ref int store, int amount)
    {
        if (store < amount) return false;
        store -= amount;
        OnChanged?.Invoke();
        return true;
    }

    private void OnEnable()
    {
        money = startingMoney;
        meat = startingMeat;
        wood = startingWood;
    }
}