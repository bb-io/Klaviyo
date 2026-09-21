using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Klaviyo.Models.Responses;

public class DownloadUniversalContentResponse
{
    [Display("Content (HTML)")]
    public FileReference Content { get; set; } = default!;

    [Display("Universal content data (JSON)")]
    public FileReference JsonFile { get; set; } = default!;
}
