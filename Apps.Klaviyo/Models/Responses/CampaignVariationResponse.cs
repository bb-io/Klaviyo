using Blackbird.Applications.Sdk.Common;

namespace Apps.Klaviyo.Models.Responses;

public class CampaignVariationResponse
{
    [Display("Campaign variation ID")]
    public string Id { get; set; } = string.Empty;

    [Display("Name")]
    public string? Name { get; set; }

    [Display("Channel")]
    public string? Channel { get; set; }

    [Display("Definition")]
    public string? Definition { get; set; }

    [Display("Created")]
    public DateTime? Created { get; set; }

    [Display("Updated")]
    public DateTime? Updated { get; set; }
}
