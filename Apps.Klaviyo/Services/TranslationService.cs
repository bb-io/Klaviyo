using Apps.Klaviyo.Api;
using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Klaviyo.Services;

public class TranslationService(KlaviyoClient client)
{
    public async Task<SearchTranslationsResponse> SearchAsync(
        SearchTranslationsRequest input,
        IEnumerable<string>? resourceTypes = null)
    {
        ValidateDateRange(input);

        var selectedChannels = NormalizeAndValidate(
            input.Channels,
            TranslationChannels.All,
            "channel");
        var requestedResourceTypes = resourceTypes?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        var selectedResourceTypes = NormalizeAndValidate(
            requestedResourceTypes is { Length: > 0 } ? requestedResourceTypes : TranslationResourceTypes.All,
            TranslationResourceTypes.All,
            "content type");

        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("page[size]", "100");

        // Klaviyo currently returns an empty collection for some documented
        // resource_type filters. Fetch the collection and apply the same
        // resource-type restriction locally for these resource types.
        if (selectedResourceTypes.Count == 1 &&
            !selectedResourceTypes[0].Equals(TranslationResourceTypes.CampaignVariation,
                StringComparison.OrdinalIgnoreCase) &&
            !selectedResourceTypes[0].Equals(TranslationResourceTypes.FlowMessage,
                StringComparison.OrdinalIgnoreCase))
            request.AddQueryParameter("filter", $"equals(resource_type,\"{selectedResourceTypes[0]}\")");

        request.AddQueryParameter("include", string.Join(',', selectedResourceTypes));

        var items = new List<TranslationResponse>();
        var visitedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            var response = await client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
            if (response is null)
                break;

            var included = response.Included
                .GroupBy(item => $"{item.Type}::{item.Id}")
                .ToDictionary(group => group.Key, group => group.First());

            items.AddRange(response.Data
                .Select(item => MapTranslation(item, included))
                .Where(item => Matches(item, selectedResourceTypes, selectedChannels, input)));

            var next = response.Links?.Next;
            if (string.IsNullOrWhiteSpace(next) || !visitedPages.Add(next))
                break;

            request = new RestRequest(next, Method.Get);
        }

        return new SearchTranslationsResponse
        {
            Items = items
                .OrderByDescending(item => item.Updated ?? item.Created ?? DateTime.MinValue)
                .ToList()
        };
    }

    public async Task<RelatedResourceDto> GetRelatedResourceAsync(
        string translationId,
        string resourceType)
    {
        if (string.IsNullOrWhiteSpace(translationId))
            throw new PluginMisconfigurationException("Translation ID is required.");

        if (!translationId.StartsWith($"{resourceType}::", StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException(
                $"Translation ID must refer to resource type '{resourceType}'.");

        var request = new RestRequest("translations/{translationId}/{resourceType}", Method.Get)
            .AddUrlSegment("translationId", translationId)
            .AddUrlSegment("resourceType", resourceType);

        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response?.Data
               ?? throw new PluginApplicationException(
                   $"Klaviyo returned an empty '{resourceType}' response for translation '{translationId}'.");
    }

    public async Task<TemplateResponse> GetTemplateAsync(string translationId)
    {
        var resource = await GetRelatedResourceAsync(translationId, TranslationResourceTypes.Template);
        var attributes = resource.Attributes;

        return new TemplateResponse
        {
            Id = resource.Id,
            Name = StringValue(attributes, "name"),
            EditorType = StringValue(attributes, "editor_type"),
            Html = StringValue(attributes, "html"),
            Text = StringValue(attributes, "text"),
            Amp = StringValue(attributes, "amp"),
            Definition = JsonValue(attributes, "definition"),
            Created = DateValue(attributes, "created"),
            Updated = DateValue(attributes, "updated")
        };
    }

    public async Task<CampaignVariationResponse> GetCampaignVariationAsync(string translationId)
    {
        var resource = await GetRelatedResourceAsync(translationId, TranslationResourceTypes.CampaignVariation);
        var attributes = resource.Attributes;

        return new CampaignVariationResponse
        {
            Id = resource.Id,
            Name = attributes.SelectToken("definition.name")?.ToString(),
            Channel = attributes.SelectToken("definition.details.channel")?.ToString()
                      ?? StringValue(attributes, "channel"),
            Definition = JsonValue(attributes, "definition"),
            Created = DateValue(attributes, "created"),
            Updated = DateValue(attributes, "updated")
        };
    }

    public async Task<UniversalContentResponse> GetUniversalContentAsync(string translationId)
    {
        var resource = await GetRelatedResourceAsync(translationId, TranslationResourceTypes.UniversalContent);
        var attributes = resource.Attributes;

        return new UniversalContentResponse
        {
            Id = resource.Id,
            Name = StringValue(attributes, "name"),
            ContentType = attributes.SelectToken("definition.content_type")?.ToString(),
            BlockType = attributes.SelectToken("definition.type")?.ToString(),
            Definition = JsonValue(attributes, "definition"),
            ScreenshotStatus = StringValue(attributes, "screenshot_status"),
            ScreenshotUrl = StringValue(attributes, "screenshot_url"),
            Created = DateValue(attributes, "created"),
            Updated = DateValue(attributes, "updated")
        };
    }

    private static TranslationResponse MapTranslation(
        TranslationDto translation,
        IReadOnlyDictionary<string, RelatedResourceDto> included)
    {
        var relationship = translation.Relationships.Properties()
            .Select(property => new
            {
                ResourceType = property.Name,
                ResourceId = property.Value["data"]?["id"]?.ToString()
            })
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.ResourceId));

        var resourceType = relationship?.ResourceType ?? GetResourceTypeFromId(translation.Id);
        var resourceId = relationship?.ResourceId ?? GetResourceIdFromId(translation.Id);
        included.TryGetValue($"{resourceType}::{resourceId}", out var relatedResource);

        return new TranslationResponse
        {
            ContentId = translation.Id,
            ResourceId = resourceId,
            ResourceType = resourceType,
            Name = GetResourceName(relatedResource?.Attributes),
            Channel = translation.Attributes.Channel,
            SourceLocale = translation.Attributes.SourceLocale,
            TargetLocales = translation.Attributes.TargetLocales,
            FallbackLocale = translation.Attributes.FallbackLocale,
            Created = translation.Attributes.Created?.UtcDateTime,
            Updated = translation.Attributes.Updated?.UtcDateTime
        };
    }

    private static bool Matches(
        TranslationResponse item,
        IReadOnlyCollection<string> resourceTypes,
        IReadOnlyCollection<string> channels,
        SearchTranslationsRequest input)
    {
        if (resourceTypes.Count > 0 && !resourceTypes.Contains(item.ResourceType, StringComparer.OrdinalIgnoreCase))
            return false;

        if (channels.Count > 0 && (item.Channel is null ||
                                  !channels.Contains(item.Channel, StringComparer.OrdinalIgnoreCase)))
            return false;

        if (input.UpdatedFrom.HasValue && (!item.Updated.HasValue || item.Updated < input.UpdatedFrom))
            return false;

        if (input.UpdatedTo.HasValue && (!item.Updated.HasValue || item.Updated > input.UpdatedTo))
            return false;

        return true;
    }

    private static List<string> NormalizeAndValidate(
        IEnumerable<string>? values,
        IEnumerable<string> supportedValues,
        string valueName)
    {
        var supported = supportedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalized = values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        var unsupported = normalized.Where(value => !supported.Contains(value)).ToArray();
        if (unsupported.Length > 0)
            throw new PluginMisconfigurationException(
                $"Unsupported {valueName} value(s): {string.Join(", ", unsupported)}.");

        return normalized;
    }

    private static void ValidateDateRange(SearchTranslationsRequest input)
    {
        if (input.UpdatedFrom.HasValue && input.UpdatedTo.HasValue && input.UpdatedFrom > input.UpdatedTo)
            throw new PluginMisconfigurationException("'Updated from' cannot be after 'Updated to'.");
    }

    private static string GetResourceTypeFromId(string translationId) =>
        translationId.Split("::", StringSplitOptions.None).FirstOrDefault() ?? string.Empty;

    private static string GetResourceIdFromId(string translationId) =>
        translationId.Split("::", StringSplitOptions.None).LastOrDefault() ?? string.Empty;

    private static string? GetResourceName(JObject? attributes) =>
        attributes?["name"]?.ToString()
        ?? attributes?.SelectToken("definition.name")?.ToString();

    private static string? StringValue(JObject attributes, string propertyName) =>
        attributes[propertyName]?.Type is JTokenType.Null or null
            ? null
            : attributes[propertyName]?.ToString();

    private static string? JsonValue(JObject attributes, string propertyName) =>
        attributes[propertyName]?.Type is JTokenType.Null or null
            ? null
            : attributes[propertyName]?.ToString(Newtonsoft.Json.Formatting.None);

    private static DateTime? DateValue(JObject attributes, string propertyName) =>
        attributes[propertyName]?.ToObject<DateTime?>();
}
