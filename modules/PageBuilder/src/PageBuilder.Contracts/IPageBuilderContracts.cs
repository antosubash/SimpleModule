namespace SimpleModule.PageBuilder.Contracts;

public interface IPageBuilderContracts
{
    Task<IEnumerable<PageSummary>> GetAllPagesAsync();
    Task<Page?> GetPageByIdAsync(PageId id);
    Task<Page?> GetPageBySlugAsync(string slug);
    Task<IEnumerable<PageSummary>> GetPublishedPagesAsync();
    Task<Page> CreatePageAsync(CreatePageRequest request);
    Task<Page> UpdatePageAsync(PageId id, UpdatePageRequest request);
    Task<Page> UpdatePageContentAsync(PageId id, UpdatePageContentRequest request);
    Task DeletePageAsync(PageId id);
    Task<Page> PublishPageAsync(PageId id);
    Task<Page> UnpublishPageAsync(PageId id);
}
