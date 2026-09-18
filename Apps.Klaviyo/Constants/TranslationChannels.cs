namespace Apps.Klaviyo.Constants;

public static class TranslationChannels
{
    public const string Email = "email";
    public const string Sms = "sms";
    public const string MobilePush = "mobile_push";
    public const string WhatsApp = "whatsapp";

    public static readonly string[] All = [Email, Sms, MobilePush, WhatsApp];
}
