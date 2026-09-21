using Apps.Klaviyo.Handlers;
using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Identifiers;

public class CampaignVariationIdentifier
{
    [Display("Channel", Description = "Channel of the campaign variation. If omitted, it is inferred from the translation or campaign variation.")]
    [StaticDataSource(typeof(CampaignVariationChannelDataSourceHandler))]
    public string? Channel { get; set; }

    [Display("Campaign variation ID", Description = "ID of the campaign variation.")]
    [DataSource(typeof(CampaignVariationDataHandler))]
    public string CampaignVariationId { get; set; } = string.Empty;
}
