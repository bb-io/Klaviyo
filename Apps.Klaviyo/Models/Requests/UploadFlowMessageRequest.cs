using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Klaviyo.Models.Requests;

public class UploadFlowMessageRequest
{
    [Display("Flow message ID", Description = "Optional. Flow message ID or flow-message::email:: translation ID. If omitted, it is read from the downloaded HTML metadata.")]
    [DataSource(typeof(FlowMessageDataHandler))]
    public string? FlowMessageId { get; set; }

    [Display("Target locale", Description = "Existing or new locale to upload the translated HTML into, for example fr.")]
    [DataSource(typeof(UploadFlowMessageLocaleDataHandler))]
    public string Locale { get; set; } = string.Empty;

    [Display("Source locale", Description = "Optional. Required only when translations have not been configured for this flow message yet.")]
    public string? SourceLocale { get; set; }

    [Display("Content file", Description = "Translated HTML file from Download flow message.")]
    public FileReference Content { get; set; } = default!;
}
