using Apps.Klaviyo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Models.Identifiers;

public class FlowMessageIdentifier
{
    [Display("Flow message ID", Description = "Flow message ID or flow-message::email:: translation ID.")]
    [DataSource(typeof(FlowMessageDataHandler))]
    public string FlowMessageId { get; set; } = string.Empty;
}
