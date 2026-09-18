using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Runtime.CompilerServices;

namespace Apps.Klaviyo.Api;

public class KlaviyoClient : BlackBirdRestClient
{
    private const string ApiRevision = "2026-07-15.pre";
    private const int MaxFallbackErrorLength = 1000;

    public KlaviyoClient(IEnumerable<AuthenticationCredentialsProvider> creds) : base(new()
    {
        BaseUrl = new Uri("https://a.klaviyo.com/api/"),
    })
    {
        var apiKey = creds.Get(CredsNames.ApiKey).Value;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new PluginMisconfigurationException(
                "Private API key cannot be empty. Add the key to the connection.");

        this.AddDefaultHeader("Authorization", $"Klaviyo-API-Key {apiKey.Trim()}");
        this.AddDefaultHeader("Accept", "application/vnd.api+json");
        this.AddDefaultHeader("revision", ApiRevision);
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        var content = (await ExecuteWithErrorHandling(request)).Content;
        if (string.IsNullOrWhiteSpace(content))
            throw new PluginApplicationException(
                $"Returned an empty response for {typeof(T).Name}.");

        return JsonConvert.DeserializeObject<T>(content, JsonSettings)
               ?? throw new PluginApplicationException(
                   $"Returned an invalid response for {typeof(T).Name}.");
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

    public async IAsyncEnumerable<JsonApiListResponse<T>> PaginateAsync<T>(
        RestRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var visitedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await ExecuteWithErrorHandling<JsonApiListResponse<T>>(request);
            yield return response;

            var next = response.Links?.Next;
            if (string.IsNullOrWhiteSpace(next) || !visitedPages.Add(next))
                yield break;

            request = new RestRequest(next, Method.Get);
        }
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var fallbackMessage = !string.IsNullOrWhiteSpace(response.Content)
            ? response.Content.Trim()
            : !string.IsNullOrWhiteSpace(response.ErrorMessage)
                ? response.ErrorMessage.Trim()
                : response.StatusCode.ToString();
        if (fallbackMessage.Length > MaxFallbackErrorLength)
            fallbackMessage = fallbackMessage[..(MaxFallbackErrorLength - 3)] + "...";

        if (string.IsNullOrWhiteSpace(response.Content))
            return new PluginApplicationException($"Request failed: {fallbackMessage}");

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
                    $"Request failed: {string.Join("; ", messages)}");
        }
        catch (JsonException)
        {
            // Preserve non-JSON error responses from proxies and gateways.
        }

        return new PluginApplicationException($"Request failed: {fallbackMessage}");
    }
}
