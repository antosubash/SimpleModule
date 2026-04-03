namespace SimpleModule.Email.Contracts;

public interface IEmailContracts
{
    Task<EmailMessage> SendEmailAsync(SendEmailRequest request);
    Task<EmailMessage> SendTemplatedEmailAsync(
        string templateSlug,
        string to,
        Dictionary<string, string> variables
    );
    Task<IEnumerable<EmailMessage>> GetAllMessagesAsync();
    Task<EmailMessage?> GetMessageByIdAsync(EmailMessageId id);
    Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync();
    Task<EmailTemplate?> GetTemplateByIdAsync(EmailTemplateId id);
    Task<EmailTemplate?> GetTemplateBySlugAsync(string slug);
    Task<EmailTemplate> CreateTemplateAsync(CreateEmailTemplateRequest request);
    Task<EmailTemplate> UpdateTemplateAsync(EmailTemplateId id, UpdateEmailTemplateRequest request);
    Task DeleteTemplateAsync(EmailTemplateId id);
}
