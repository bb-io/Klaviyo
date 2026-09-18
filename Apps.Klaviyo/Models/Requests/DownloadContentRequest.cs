using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadContentRequest : ContentTypeFilter, IDownloadContentInput
{
    [Display("Content ID", Description = "ID of the selected content item.")]
    [DataSource(typeof(DownloadContentDataHandler))]
    public string ContentId { get; set; } = string.Empty;

    [Display("Locale", Description = "Locale to download. Omit it to download the source content.")]
    [DataSource(typeof(ContentLocaleDataHandler))]
    public string? Locale { get; set; }
}
