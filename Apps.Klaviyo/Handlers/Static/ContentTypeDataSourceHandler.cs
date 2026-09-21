using Apps.Klaviyo.Constants;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Handlers.Static;

public class ContentTypeDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(TranslationResourceTypes.Template, TranslationResourceDisplayNames.Template),
        new(TranslationResourceTypes.CampaignVariation, TranslationResourceDisplayNames.CampaignVariation),
        new(TranslationResourceTypes.FlowMessage, TranslationResourceDisplayNames.FlowMessage),
        new(TranslationResourceTypes.UniversalContent, TranslationResourceDisplayNames.UniversalContent)
    ];
}
