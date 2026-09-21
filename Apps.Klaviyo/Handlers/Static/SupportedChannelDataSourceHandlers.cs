using Apps.Klaviyo.Constants;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Handlers.Static;

public abstract class SupportedChannelDataSourceHandler(IEnumerable<string> supportedChannels)
    : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() => supportedChannels.Select(channel =>
        new DataSourceItem(channel, channel switch
        {
            TranslationChannels.Email => "Email",
            TranslationChannels.Sms => "SMS",
            TranslationChannels.MobilePush => "Mobile push",
            TranslationChannels.WhatsApp => "WhatsApp",
            _ => channel
        }));
}

public class CampaignVariationChannelDataSourceHandler()
    : SupportedChannelDataSourceHandler(TranslationChannels.CampaignVariation);

public class FlowMessageChannelDataSourceHandler()
    : SupportedChannelDataSourceHandler(TranslationChannels.FlowMessage);

public class TemplateChannelDataSourceHandler()
    : SupportedChannelDataSourceHandler(TranslationChannels.Template);
