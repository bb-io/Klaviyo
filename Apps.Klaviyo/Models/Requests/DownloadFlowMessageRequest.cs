using Apps.Klaviyo.Handlers;
using Apps.Klaviyo.Handlers.Static;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Requests;

public class DownloadFlowMessageRequest
{
    [Display("Channel", Description = "Channel of the flow message. If omitted, it is inferred from its translation.")]
    [StaticDataSource(typeof(FlowMessageChannelDataSourceHandler))]
    public string? Channel { get; set; }

    [Display("Flow message ID", Description = "ID of the flow message.")]
    [DataSource(typeof(FlowMessageDataHandler))]
    public string FlowMessageId { get; set; } = string.Empty;

    [Display("Locale", Description = "Locale to download. Omit it to download the source content.")]
    [DataSource(typeof(FlowMessageLocaleDataHandler))]
    public string? Locale { get; set; }
}
