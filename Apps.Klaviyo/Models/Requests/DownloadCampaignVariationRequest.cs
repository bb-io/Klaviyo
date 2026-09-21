using Apps.Klaviyo.Handlers;
using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadCampaignVariationRequest
{
    [Display("Channel", Description = "Channel of the campaign variation. If omitted, it is inferred from its translation.")]
    [StaticDataSource(typeof(CampaignVariationChannelDataSourceHandler))]
    public string? Channel { get; set; }

    [Display("Campaign variation ID", Description = "ID of the campaign variation.")]
    [DataSource(typeof(CampaignVariationDataHandler))]
    public string CampaignVariationId { get; set; } = string.Empty;

    [Display("Locale", Description = "Locale to download. Omit it to download the source content.")]
    [DataSource(typeof(CampaignVariationLocaleDataHandler))]
    public string? Locale { get; set; }
}
