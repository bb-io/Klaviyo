using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadUniversalContentRequest
{
    [Display("Universal content ID", Description = "Universal content ID or template-universal-content::email:: translation ID.")]
    [DataSource(typeof(UniversalContentDataHandler))]
    public string UniversalContentId { get; set; } = string.Empty;

    [Display("Locale", Description = "Optional locale to download. Omit it to download the source content.")]
    [DataSource(typeof(UniversalContentLocaleDataHandler))]
    public string? Locale { get; set; }
}
