using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Klaviyo.Models.Requests;

public class UploadFlowMessageRequest
{
    [Display("Flow message ID", Description = "ID of the flow message. If omitted, it is read from the downloaded content metadata.")]
    [DataSource(typeof(FlowMessageDataHandler))]
    public string? FlowMessageId { get; set; }

    [Display("Target locale", Description = "Existing or new locale to upload the translated content into, for example fr.")]
    [DataSource(typeof(UploadFlowMessageLocaleDataHandler))]
    public string Locale { get; set; } = string.Empty;

    [Display("Source locale", Description = "Required only when translations have not been configured for this flow message yet.")]
    public string? SourceLocale { get; set; }

    [Display("Content file", Description = "Translated content from Download flow message.")]
    public FileReference Content { get; set; } = default!;
}
