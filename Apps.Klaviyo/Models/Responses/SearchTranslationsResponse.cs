using Blackbird.Applications.Sdk.Common;

namespace Apps.Klaviyo.Models.Responses;

public class SearchTranslationsResponse
{
    [Display("Items")]
    public List<TranslationResponse> Items { get; set; } = [];

    [Display("Total count")]
    public int TotalCount => Items.Count;
}
