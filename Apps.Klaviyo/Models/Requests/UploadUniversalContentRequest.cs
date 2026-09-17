using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Klaviyo.Models.Requests;

public class UploadUniversalContentRequest
{
    [Display("Universal content ID", Description = "Optional. Universal content ID or template-universal-content::email:: translation ID. If omitted, it is read from the downloaded HTML metadata.")]
    [DataSource(typeof(UniversalContentDataHandler))]
    public string? UniversalContentId { get; set; }

    [Display("Target locale", Description = "Existing or new locale to upload the translated HTML into, for example fr.")]
    [DataSource(typeof(UploadUniversalContentLocaleDataHandler))]
    public string Locale { get; set; } = string.Empty;

    [Display("Source locale", Description = "Optional. Required only when translations have not been configured for this universal content yet.")]
    public string? SourceLocale { get; set; }

    [Display("Content file", Description = "Translated HTML file from Download universal content.")]
    public FileReference Content { get; set; } = default!;
}
