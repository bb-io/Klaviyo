using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Klaviyo.Models.Requests;

public class SearchTranslationsRequest
{
    [Display("Channels", Description = "Return only translations for the selected channels.")]
    [StaticDataSource(typeof(ChannelDataSourceHandler))]
    public IEnumerable<string>? Channels { get; set; }

    [Display("Updated from", Description = "Return translations updated at or after this date and time.")]
    public DateTime? UpdatedFrom { get; set; }

    [Display("Updated to", Description = "Return translations updated at or before this date and time.")]
    public DateTime? UpdatedTo { get; set; }
}
