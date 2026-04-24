using Microsoft.EntityFrameworkCore;
using SimpleModule.Items.Contracts;

namespace SimpleModule.Items;

public class ItemService(ItemsDbContext db) : IItemContracts
{
    public async Task<IEnumerable<Item>> GetAllItemsAsync() =>
        await db.Items.ToListAsync().ConfigureAwait(false);
}
