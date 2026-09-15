using Apps.Klaviyo.Api;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Klaviyo.Events.Webhooks.Handlers;

public class ItemCreatedHandler(InvocationContext invocationContext) : BaseInvocable(invocationContext), IWebhookEventHandler
{
    private const string Topic = "item.created";

    public async Task SubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProvider,
        Dictionary<string, string> values)
    {
        var client = new KlaviyoClient(authenticationCredentialsProvider);
        var payload = new
        {
            url = values["payloadUrl"],
            events = new[] { Topic }
        };

        var request = new RestRequest("/webhooks", Method.Post)
            .AddJsonBody(JsonConvert.SerializeObject(payload));
        await client.ExecuteWithErrorHandling(request);
    }

    public async Task UnsubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProvider,
        Dictionary<string, string> values)
    {
        var client = new KlaviyoClient(authenticationCredentialsProvider);
        var listRequest = new RestRequest("/webhooks", Method.Get);
        var response = await client.ExecuteWithErrorHandling(listRequest);

        var webhooks = JsonConvert.DeserializeObject<List<WebhookSubscription>>(response.Content ?? "[]") ?? [];
        var subscription = webhooks.FirstOrDefault(x => x.Url == values["payloadUrl"]);
        if (subscription is null)
            return;

        var deleteRequest = new RestRequest($"/webhooks/{subscription.Id}", Method.Delete);
        await client.ExecuteWithErrorHandling(deleteRequest);
    }

    private class WebhookSubscription
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;
        [JsonProperty("url")] public string Url { get; set; } = string.Empty;
    }
}
