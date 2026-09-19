using Apps.Klaviyo.Handlers;
using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Klaviyo.Models.Requests;

public class UploadContentRequest : IUploadContentInput
{
    [Display("Content type")]
    [StaticDataSource(typeof(ContentTypeDataSourceHandler))]
    public string ContentType { get; set; } = string.Empty;

    [Display("Content ID", Description = "If omitted, the ID is read from the downloaded content metadata.")]
    [DataSource(typeof(UploadContentDataHandler))]
    public string? ContentId { get; set; }

    [Display("Target locale", Description = "Existing or new locale to upload the translated content into, for example fr.")]
    [DataSource(typeof(UploadContentLocaleDataHandler))]
    public string Locale { get; set; } = string.Empty;

    [Display("Source locale", Description = "Required only when translations have not been configured for the selected content yet.")]
    public string? SourceLocale { get; set; }

    [Display("Content file", Description = "Translated content from Download content.")]
    public FileReference Content { get; set; } = default!;
}
