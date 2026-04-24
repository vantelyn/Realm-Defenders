public interface IRecipe
{
    string DisplayName { get; }
    string Description { get; }
    int WoodCost { get; }
    int MeatCost { get; }
    int MoneyCost { get; }
}