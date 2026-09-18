using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Identifiers;

public class UniversalContentIdentifier
{
    [Display("Universal content ID", Description = "Universal content ID or template-universal-content::email:: translation ID.")]
    [DataSource(typeof(UniversalContentDataHandler))]
    public string UniversalContentId { get; set; } = string.Empty;
}
