using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Actions;

[ActionList("Flow messages")]
public class FlowMessageActions(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [Action("Search flow messages", Description = "Searches translations associated with flow messages.")]
    public Task<SearchTranslationsResponse> SearchFlowMessages(
        [ActionParameter] SearchTranslationsRequest input) =>
        new TranslationService(Client).SearchAsync(input, [TranslationResourceTypes.FlowMessage]);
}
