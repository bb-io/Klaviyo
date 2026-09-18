using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadCampaignVariationRequest
{
    [Display("Campaign variation ID", Description = "Campaign variation ID or campaign-variation::email:: translation ID.")]
    [DataSource(typeof(CampaignVariationDataHandler))]
    public string CampaignVariationId { get; set; } = string.Empty;

    [Display("Locale", Description = "Locale to download. Omit it to download the source content.")]
    [DataSource(typeof(CampaignVariationLocaleDataHandler))]
    public string? Locale { get; set; }
}
