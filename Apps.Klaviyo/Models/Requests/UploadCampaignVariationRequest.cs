using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Klaviyo.Models.Requests;

public class UploadCampaignVariationRequest
{
    [Display("Campaign variation ID", Description = "Campaign variation ID or campaign-variation::email:: translation ID.")]
    [DataSource(typeof(CampaignVariationDataHandler))]
    public string CampaignVariationId { get; set; } = string.Empty;

    [Display("Target locale", Description = "Existing or new locale to upload the translated HTML into, for example fr.")]
    [DataSource(typeof(UploadCampaignVariationLocaleDataHandler))]
    public string Locale { get; set; } = string.Empty;

    [Display("Source locale", Description = "Optional. Required only when translations have not been configured for this campaign variation yet.")]
    public string? SourceLocale { get; set; }

    [Display("Content file", Description = "Translated HTML file from Download campaign variation.")]
    public FileReference Content { get; set; } = default!;
}
