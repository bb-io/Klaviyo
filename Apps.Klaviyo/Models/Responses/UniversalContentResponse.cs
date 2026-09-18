using Blackbird.Applications.Sdk.Common;

namespace Apps.Klaviyo.Models.Responses;

public class UniversalContentResponse
{
    [Display("Universal content ID")]
    public string Id { get; set; } = string.Empty;

    [Display("Name")]
    public string? Name { get; set; }

    [Display("Content type")]
    public string? ContentType { get; set; }

    [Display("Block type")]
    public string? BlockType { get; set; }

    [Display("Definition")]
    public string? Definition { get; set; }

    [Display("Screenshot status")]
    public string? ScreenshotStatus { get; set; }

    [Display("Screenshot URL")]
    public string? ScreenshotUrl { get; set; }

    [Display("Created at")]
    public DateTime? Created { get; set; }

    [Display("Updated at")]
    public DateTime? Updated { get; set; }
}
