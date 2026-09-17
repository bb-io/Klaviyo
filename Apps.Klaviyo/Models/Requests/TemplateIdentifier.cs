using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class TemplateIdentifier
{
    [Display("Template ID", Description = "Template ID or template::email:: translation ID.")]
    [DataSource(typeof(TemplateDataHandler))]
    public string TemplateId { get; set; } = string.Empty;
}
