using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Klaviyo.Models.Responses;

public class ContentUpdatedItem : IDownloadContentInput
{
    [Display("Content ID")]
    public string ContentId { get; set; } = string.Empty;

    [Display("Content type")]
    public string ContentType { get; set; } = string.Empty;

    [Display("Resource ID")]
    public string ResourceId { get; set; } = string.Empty;

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
