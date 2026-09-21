namespace Apps.Klaviyo.Constants;

public static class TranslationChannels
{
    public const string Email = "email";
    public const string Sms = "sms";
    public const string MobilePush = "mobile_push";
    public const string WhatsApp = "whatsapp";

    public static readonly string[] All = [Email, Sms, MobilePush, WhatsApp];

    public static readonly string[] CampaignVariation = [Email, Sms, MobilePush];
    public static readonly string[] FlowMessage = [Email, Sms, MobilePush];
    public static readonly string[] Template = [Email, WhatsApp];
    public static readonly string[] UniversalContent = [Email];

    public static IReadOnlyCollection<string> ForResourceType(string resourceType) => resourceType switch
    {
        TranslationResourceTypes.CampaignVariation => CampaignVariation,
        TranslationResourceTypes.FlowMessage => FlowMessage,
        TranslationResourceTypes.Template => Template,
        TranslationResourceTypes.UniversalContent => UniversalContent,
        _ => []
    };
}
