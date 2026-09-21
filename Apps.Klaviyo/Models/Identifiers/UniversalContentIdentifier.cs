using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Identifiers;

public class UniversalContentIdentifier
{
    [Display("Universal content ID", Description = "ID of the universal content.")]
    [DataSource(typeof(UniversalContentDataHandler))]
    public string UniversalContentId { get; set; } = string.Empty;
}
