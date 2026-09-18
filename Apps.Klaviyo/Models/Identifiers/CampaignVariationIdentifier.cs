using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Identifiers;

public class CampaignVariationIdentifier
{
    [Display("Campaign variation ID", Description = "ID of the campaign variation.")]
    [DataSource(typeof(CampaignVariationDataHandler))]
    public string CampaignVariationId { get; set; } = string.Empty;
}
