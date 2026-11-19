namespace Apps.Klaviyo.Services.Content.Models;

public class ContentDownloadServiceRequest
{
    public required string ContentId { get; init; }

    public string? Channel { get; init; }

    public string? Locale { get; init; }
}
