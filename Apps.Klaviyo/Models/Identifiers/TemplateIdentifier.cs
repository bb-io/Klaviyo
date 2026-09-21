using Apps.Klaviyo.Handlers;
using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Identifiers;

public class TemplateIdentifier
{
    [Display("Channel", Description = "Template channel. If omitted, it is inferred from the translation.")]
    [StaticDataSource(typeof(TemplateChannelDataSourceHandler))]
    public string? Channel { get; set; }

    [Display("Template ID", Description = "ID of the template.")]
    [DataSource(typeof(TemplateDataHandler))]
    public string TemplateId { get; set; } = string.Empty;
}
