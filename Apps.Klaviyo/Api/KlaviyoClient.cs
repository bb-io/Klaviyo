using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using HtmlAgilityPack;
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
        if (string.IsNullOrWhiteSpace(response.Content))
        {
            return new PluginApplicationException(!string.IsNullOrWhiteSpace(response.ErrorMessage)
                ? TruncateErrorMessage(response.ErrorMessage)
                : $"Unknown error occurred. Status code: {response.StatusCode}, {response.StatusDescription}");
        }

        if (response.ContentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true ||
            response.Content.TrimStart().StartsWith("<", StringComparison.Ordinal))
        {
            return new PluginApplicationException(
                $"Expected JSON but received HTML ({response.StatusCode}). {ExtractHtmlErrorMessage(response.Content)}");
        }

        var fallbackException = new PluginApplicationException(
            $"Request failed ({response.StatusCode}): {TruncateErrorMessage(response.Content)}");

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
            return fallbackException;
        }

        return fallbackException;
    }

    private static string ExtractHtmlErrorMessage(string htmlContent)
    {
        var document = new HtmlDocument();
        document.LoadHtml(htmlContent);

        var ignoredNodes = document.DocumentNode.SelectNodes("//script|//style");
        if (ignoredNodes is not null)
        {
            foreach (var node in ignoredNodes)
                node.Remove();
        }

        var messageParts = new[]
        {
            document.DocumentNode.SelectSingleNode("//title")?.InnerText,
            document.DocumentNode.SelectSingleNode("//h1")?.InnerText,
            document.DocumentNode.SelectSingleNode("//p")?.InnerText
        }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => HtmlEntity.DeEntitize(part!).Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var message = messageParts.Length > 0
            ? string.Join("; ", messageParts)
            : HtmlEntity.DeEntitize(document.DocumentNode.InnerText);

        return string.IsNullOrWhiteSpace(message)
            ? "No error details found in HTML response."
            : TruncateErrorMessage(message);
    }

    private static string TruncateErrorMessage(string message)
    {
        message = message.Trim();
        return message.Length > MaxFallbackErrorLength
            ? message[..(MaxFallbackErrorLength - 3)] + "..."
            : message;
    }
}
