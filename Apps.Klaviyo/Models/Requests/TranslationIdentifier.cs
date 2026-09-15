using Blackbird.Applications.Sdk.Common;

namespace Apps.Klaviyo.Models.Requests;

public class TranslationIdentifier
{
    [Display("Translation ID", Description = "The composite Klaviyo translation ID returned by a search action.")]
    public string TranslationId { get; set; } = string.Empty;
}
