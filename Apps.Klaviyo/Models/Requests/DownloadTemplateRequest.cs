using Apps.Klaviyo.Handlers;
using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadTemplateRequest
{
    [Display("Channel", Description = "Template channel. If omitted, it is inferred from its translation or defaults to email for an untranslated template.")]
    [StaticDataSource(typeof(TemplateChannelDataSourceHandler))]
    public string? Channel { get; set; }

    [Display("Template ID", Description = "ID of the template.")]
    [DataSource(typeof(TemplateDataHandler))]
    public string TemplateId { get; set; } = string.Empty;

    [Display("Locale", Description = "Locale to download. Omit it to download the source template.")]
    [DataSource(typeof(TemplateLocaleDataHandler))]
    public string? Locale { get; set; }
}
