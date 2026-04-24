namespace SimpleModule.Items.Contracts;

public interface IItemContracts
{
    Task<IEnumerable<Item>> GetAllItemsAsync();
}
