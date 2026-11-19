using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Klaviyo.Services.Content.Models;

public class ContentUploadServiceRequest
{
    public string? ContentId { get; init; }

    public required string Locale { get; init; }

    public string? SourceLocale { get; init; }

    public required FileReference Content { get; init; }

    public TranslationHtmlFile? ParsedContent { get; init; }
}
