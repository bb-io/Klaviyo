using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Klaviyo.Models.Responses;

public class DownloadFlowMessageResponse
{
    [Display("Content (HTML)")]
    public FileReference Content { get; set; } = default!;

    [Display("Flow message data (JSON)")]
    public FileReference JsonFile { get; set; } = default!;
}
