using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Klaviyo.Models.Responses;

public class DownloadContentResponse : IDownloadContentOutput
{
    [Display("Content (HTML)")]
    public FileReference Content { get; set; } = default!;

    [Display("Content data (JSON)")]
    public FileReference JsonFile { get; set; } = default!;
}
