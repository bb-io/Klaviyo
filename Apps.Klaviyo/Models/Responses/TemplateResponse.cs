using Blackbird.Applications.Sdk.Common;

namespace Apps.Klaviyo.Models.Responses;

public class TemplateResponse
{
    [Display("Template ID")]
    public string Id { get; set; } = string.Empty;

    [Display("Name")]
    public string? Name { get; set; }

    [Display("Editor type")]
    public string? EditorType { get; set; }

    [Display("HTML")]
    public string? Html { get; set; }

    [Display("Plain text")]
    public string? Text { get; set; }

    [Display("AMP")]
    public string? Amp { get; set; }

    [Display("Definition")]
    public string? Definition { get; set; }

    [Display("Created")]
    public DateTime? Created { get; set; }

    [Display("Updated")]
    public DateTime? Updated { get; set; }
}
