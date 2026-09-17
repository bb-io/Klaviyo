using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class CampaignVariationIdentifier
{
    [Display("Campaign variation ID", Description = "Campaign variation ID or campaign-variation::email:: translation ID.")]
    [DataSource(typeof(CampaignVariationDataHandler))]
    public string CampaignVariationId { get; set; } = string.Empty;
}
