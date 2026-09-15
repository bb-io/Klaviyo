using Apps.Klaviyo.Constants;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Handlers.Static;

public class ContentTypeDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(TranslationResourceTypes.Template, "Template"),
        new(TranslationResourceTypes.CampaignVariation, "Campaign variation"),
        new(TranslationResourceTypes.FlowMessage, "Flow message"),
        new(TranslationResourceTypes.UniversalContent, "Universal content")
    ];
}
