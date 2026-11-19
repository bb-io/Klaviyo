using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Klaviyo.Services.Content.Models;

public class ContentDownloadResult
{
    public required FileReference Content { get; init; }

    public required FileReference JsonFile { get; init; }
}
