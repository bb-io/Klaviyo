using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadFlowMessageRequest
{
    [Display("Flow message ID", Description = "ID of the flow message.")]
    [DataSource(typeof(FlowMessageDataHandler))]
    public string FlowMessageId { get; set; } = string.Empty;

    [Display("Locale", Description = "Locale to download. Omit it to download the source content.")]
    [DataSource(typeof(FlowMessageLocaleDataHandler))]
    public string? Locale { get; set; }
}
