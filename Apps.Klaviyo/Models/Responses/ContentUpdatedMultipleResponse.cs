using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Klaviyo.Models.Responses;

public class ContentUpdatedMultipleResponse : IMultiDownloadableContentOutput<ContentUpdatedItem>
{
    [Display("Items")]
    public List<ContentUpdatedItem> Items { get; set; } = [];

    [Display("Total count", Description = "The number of content items in this event.")]
    public int TotalCount => Items.Count;
}
