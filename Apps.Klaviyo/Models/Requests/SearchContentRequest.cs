using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Klaviyo.Models.Requests;

public class SearchContentRequest : SearchTranslationsRequest
{
    [Display("Content types", Description = "Return only the selected content types. All types are returned by default.")]
    [StaticDataSource(typeof(ContentTypeDataSourceHandler))]
    public IEnumerable<string>? ContentTypes { get; set; }
}
