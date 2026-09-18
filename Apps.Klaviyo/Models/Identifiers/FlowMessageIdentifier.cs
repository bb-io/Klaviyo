using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Identifiers;

public class FlowMessageIdentifier
{
    [Display("Flow message ID", Description = "ID of the flow message.")]
    [DataSource(typeof(FlowMessageDataHandler))]
    public string FlowMessageId { get; set; } = string.Empty;
}
