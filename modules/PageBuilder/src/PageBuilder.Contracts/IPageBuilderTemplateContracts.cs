namespace SimpleModule.PageBuilder.Contracts;

public interface IPageBuilderTemplateContracts
{
    Task<IEnumerable<PageTemplate>> GetAllTemplatesAsync();
    Task<PageTemplate> CreateTemplateAsync(CreatePageTemplateRequest request);
    Task DeleteTemplateAsync(PageTemplateId id);
}
