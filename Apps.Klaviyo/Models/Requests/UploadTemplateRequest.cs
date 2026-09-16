using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class UploadTemplateRequest
{
    [Display("Template ID", Description = "Template ID (U7pVWU) or template::email:: translation ID.")]
    [DataSource(typeof(TemplateDataHandler))]
    public string TemplateId { get; set; } = string.Empty;

    [Display("Target locale", Description = "Required existing or new locale to upload the translated HTML into, for example fr.")]
    public string Locale { get; set; } = string.Empty;

    [Display("Source locale", Description = "Used only when translations must be enabled for this template.")]
    public string SourceLocale { get; set; } = "en";

    [Display("Content file", Description = "Translated HTML file from Download template.")]
    public FileReference Content { get; set; } = default!;
}
