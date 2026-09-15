using Apps.Klaviyo.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Klaviyo.Api;

public class KlaviyoClient : BlackBirdRestClient
{
    private const string ApiRevision = "2026-07-15.pre";

    public KlaviyoClient(IEnumerable<AuthenticationCredentialsProvider> creds) : base(new()
    {
        BaseUrl = new Uri("https://a.klaviyo.com/api/"),
    })
    {
        var apiKey = creds.Get(CredsNames.ApiKey).Value;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Klaviyo private API key cannot be empty.");

        this.AddDefaultHeader("Authorization", $"Klaviyo-API-Key {apiKey.Trim()}");
        this.AddDefaultHeader("Accept", "application/vnd.api+json");
        this.AddDefaultHeader("revision", ApiRevision);
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;
        T val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
        if (val == null)
        {
            throw new Exception($"Could not parse {content} to {typeof(T)}");
        }

        return val;
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse restResponse = await ExecuteAsync(request);
        if (!restResponse.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(restResponse);
        }

        return restResponse;
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var fallbackMessage = response.ErrorMessage
                              ?? response.Content
                              ?? response.StatusCode.ToString();

        if (string.IsNullOrWhiteSpace(response.Content))
            return new PluginApplicationException($"Klaviyo API request failed: {fallbackMessage}");

        try
        {
            var payload = JObject.Parse(response.Content);
            var messages = payload["errors"]?
                .Children()
                .Select(error => error["detail"]?.ToString() ?? error["title"]?.ToString())
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToArray();

            if (messages?.Length > 0)
                return new PluginApplicationException(
                    $"Klaviyo API request failed: {string.Join("; ", messages)}");
        }
        catch (JsonException)
        {
            // Preserve non-JSON error responses from proxies and gateways.
        }

        return new PluginApplicationException($"Klaviyo API request failed: {fallbackMessage}");
    }
}
