using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Klaviyo.Models.Responses;

public class SearchTranslationsResponse
{
    [Display("Items")]
    public List<TranslationResponse> Items { get; set; } = [];

    [Display("Total count")]
    public int TotalCount => Items.Count;
}

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

    [Display("Created")]
    public DateTime? Created { get; set; }

    [Display("Updated")]
    public DateTime? Updated { get; set; }
}
