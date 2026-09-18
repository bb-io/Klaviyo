using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Klaviyo.Models.Responses;

public class TranslationResponse : IDownloadContentInput
{
    [Display("Translation ID")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Resource ID")]
    public string ResourceId { get; set; } = string.Empty;

    [Display("Resource type")]
    public string ResourceType { get; set; } = string.Empty;

    [Display("Name")]
    public string? Name { get; set; }

    [Display("Channel")]
    public string? Channel { get; set; }

    [Display("Source locale")]
    public string? SourceLocale { get; set; }

    [Display("Target locales")]
    public IEnumerable<string> TargetLocales { get; set; } = [];

    [Display("Fallback locale")]
    public string? FallbackLocale { get; set; }

    [Display("Created at")]
    public DateTime? Created { get; set; }

    [Display("Updated at")]
    public DateTime? Updated { get; set; }
}
