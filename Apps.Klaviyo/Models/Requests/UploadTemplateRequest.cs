using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class UploadTemplateRequest
{
    [Display("Template ID", Description = "ID of the template. If omitted, it is read from the downloaded content metadata.")]
    [DataSource(typeof(TemplateDataHandler))]
    public string? TemplateId { get; set; }

    [Display("Target locale", Description = "Required existing or new locale to upload the translated content into, for example fr.")]
    [DataSource(typeof(UploadTemplateLocaleDataHandler))]
    public string Locale { get; set; } = string.Empty;

    [Display("Source locale", Description = "Required only when translations have not been configured for this template yet.")]
    public string? SourceLocale { get; set; }

    [Display("Content file", Description = "Translated content from Download template.")]
    public FileReference Content { get; set; } = default!;
}
