using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadTemplateRequest
{
    [Display("Template ID", Description = "Template ID (U7pVWU) or template::email:: translation ID.")]
    [DataSource(typeof(TemplateDataHandler))]
    public string TemplateId { get; set; } = string.Empty;

    [Display("Locale", Description = "Locale to download. Omit it to download the source template.")]
    [DataSource(typeof(TemplateLocaleDataHandler))]
    public string? Locale { get; set; }
}
