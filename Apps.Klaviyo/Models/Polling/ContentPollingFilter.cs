using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Klaviyo.Models.Polling;

public class ContentPollingFilter
{
    [Display("Content types", Description = "Only trigger for the selected content types. All types are monitored by default.")]
    [StaticDataSource(typeof(ContentTypeDataSourceHandler))]
    public IEnumerable<string>? ContentTypes { get; set; }
}
