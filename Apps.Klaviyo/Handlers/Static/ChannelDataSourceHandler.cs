using Apps.Klaviyo.Constants;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Handlers.Static;

public class ChannelDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(TranslationChannels.Email, "Email"),
        new(TranslationChannels.Sms, "SMS"),
        new(TranslationChannels.MobilePush, "Mobile push"),
        new(TranslationChannels.WhatsApp, "WhatsApp")
    ];
}
