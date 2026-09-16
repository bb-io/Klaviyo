using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadTemplateRequest
{
    [Display("Template ID", Description = "Template ID (U7pVWU) or template::email:: translation ID.")]
    [DataSource(typeof(TemplateDataHandler))]
    public string TemplateId { get; set; } = string.Empty;

    [Display("Target locale", Description = "Locale to translate into, for example fr. It may be an existing or new locale.")]
    public string Locale { get; set; } = string.Empty;

    [Display("Source locale", Description = "Used when translations have not been enabled for this template yet.")]
    public string SourceLocale { get; set; } = "en";
}
