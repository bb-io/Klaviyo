using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Klaviyo.Models.Requests;

public class ContentTypeFilter
{
    [Display("Content type")]
    [StaticDataSource(typeof(ContentTypeDataSourceHandler))]
    public string ContentType { get; set; } = string.Empty;
}
