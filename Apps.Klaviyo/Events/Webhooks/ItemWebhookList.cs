using System.Net;
using Apps.Klaviyo.Events.Webhooks.Handlers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;

namespace Apps.Klaviyo.Events.Webhooks;

[WebhookList("Items")]
public class ItemWebhookList(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [BlueprintEventDefinition(BlueprintEvent.ContentCreatedOrUpdated)]
    [Webhook("On item created", typeof(ItemCreatedHandler),
        Description = "Triggered when a content item is created in the third-party system.")]
    public Task<WebhookResponse<ItemResponse>> OnItemCreated(WebhookRequest webhookRequest,
        [WebhookParameter] ItemWebhookInput input)
    {
        var payload = JsonConvert.DeserializeObject<ItemWebhookPayload>(webhookRequest.Body.ToString()!);
        if (payload is null)
            throw new InvalidCastException(nameof(webhookRequest.Body));

        if (input.ContentId is not null && input.ContentId != payload.Id)
            return Preflight();

        return Task.FromResult(new WebhookResponse<ItemResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
            Result = new ItemResponse { ContentId = payload.Id }
        });
    }

    private static Task<WebhookResponse<ItemResponse>> Preflight() =>
        Task.FromResult(new WebhookResponse<ItemResponse>
        {
            HttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK),
            Result = null,
            ReceivedWebhookRequestType = WebhookRequestType.Preflight
        });
}
