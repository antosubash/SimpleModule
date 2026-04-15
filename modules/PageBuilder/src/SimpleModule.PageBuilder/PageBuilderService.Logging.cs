using Microsoft.Extensions.Logging;
using SimpleModule.PageBuilder.Contracts;

namespace SimpleModule.PageBuilder;

public sealed partial class PageBuilderService
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Page with ID {PageId} not found")]
    private static partial void LogPageNotFound(ILogger logger, PageId pageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} created: {PageTitle}")]
    private static partial void LogPageCreated(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} updated: {PageTitle}")]
    private static partial void LogPageUpdated(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} content updated")]
    private static partial void LogPageContentUpdated(ILogger logger, PageId pageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} deleted")]
    private static partial void LogPageDeleted(ILogger logger, PageId pageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} published: {PageTitle}")]
    private static partial void LogPagePublished(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Page {PageId} unpublished: {PageTitle}"
    )]
    private static partial void LogPageUnpublished(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} restored: {PageTitle}")]
    private static partial void LogPageRestored(ILogger logger, PageId pageId, string pageTitle);

    [LoggerMessage(Level = LogLevel.Information, Message = "Page {PageId} permanently deleted")]
    private static partial void LogPagePermanentlyDeleted(ILogger logger, PageId pageId);
}
