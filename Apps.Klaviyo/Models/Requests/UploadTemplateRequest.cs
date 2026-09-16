using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class UploadTemplateRequest
{
    [Display("Template ID", Description = "Optional. Template ID or template::email:: translation ID. If omitted, it is read from the downloaded HTML metadata.")]
    [DataSource(typeof(TemplateDataHandler))]
    public string? TemplateId { get; set; }

    [Display("Target locale", Description = "Required existing or new locale to upload the translated HTML into, for example fr.")]
    [DataSource(typeof(UploadTemplateLocaleDataHandler))]
    public string Locale { get; set; } = string.Empty;

    [Display("Source locale", Description = "Optional. Required only when translations have not been configured for this template yet.")]
    public string? SourceLocale { get; set; }

    [Display("Content file", Description = "Translated HTML file from Download template.")]
    public FileReference Content { get; set; } = default!;
}
