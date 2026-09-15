using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Klaviyo.Models.Responses;

public class DownloadTranslationResponse : IDownloadContentOutput
{
    [Display("Content (XLIFF)")]
    public FileReference Content { get; set; } = default!;
}
