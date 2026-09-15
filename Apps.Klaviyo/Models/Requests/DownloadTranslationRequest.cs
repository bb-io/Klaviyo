using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadTranslationRequest : IDownloadContentInput
{
    [Display("Translation ID", Description = "The composite translation ID returned by Search.")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Target locale", Description = "The locale to export, for example de or fr.")]
    public string TargetLocale { get; set; } = string.Empty;
}
