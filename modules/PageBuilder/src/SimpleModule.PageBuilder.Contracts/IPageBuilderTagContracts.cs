namespace SimpleModule.PageBuilder.Contracts;

public interface IPageBuilderTagContracts
{
    Task<IEnumerable<PageTag>> GetAllTagsAsync();
    Task<PageTag> GetOrCreateTagAsync(string name);
    Task AddTagToPageAsync(PageId pageId, string tagName);
    Task RemoveTagFromPageAsync(PageId pageId, PageTagId tagId);
}
