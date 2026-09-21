using Apps.Klaviyo.Api;
using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Responses;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Klaviyo.Helpers;

public static class TranslationResourceHelper
{
    public static async Task<RelatedResourceDto> GetRelatedResourceAsync(
        KlaviyoClient client,
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
                   $"Returned an empty '{resourceType}' response for translation '{translationId}'.");
    }

    public static async Task<TemplateResponse> GetTemplateByTranslationIdAsync(
        KlaviyoClient client,
        string translationId)
    {
        var resource = await GetRelatedResourceAsync(
            client, translationId, TranslationResourceTypes.Template);
        var attributes = resource.Attributes;

        return new TemplateResponse
        {
            Id = resource.Id,
            Name = StringValue(attributes, "name"),
            Channel = translationId.Split("::", StringSplitOptions.None).ElementAtOrDefault(1),
            EditorType = StringValue(attributes, "editor_type"),
            Text = StringValue(attributes, "text"),
            Amp = StringValue(attributes, "amp"),
            Definition = JsonValue(attributes, "definition"),
            Created = DateValue(attributes, "created"),
            Updated = DateValue(attributes, "updated")
        };
    }

    public static async Task<CampaignVariationResponse> GetCampaignVariationByTranslationIdAsync(
        KlaviyoClient client,
        string translationId)
    {
        var resource = await GetRelatedResourceAsync(
            client, translationId, TranslationResourceTypes.CampaignVariation);
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

    public static async Task<FlowMessageResponse> GetFlowMessageByTranslationIdAsync(
        KlaviyoClient client,
        string translationId)
    {
        var resource = await GetRelatedResourceAsync(
            client, translationId, TranslationResourceTypes.FlowMessage);
        var attributes = resource.Attributes;

        return new FlowMessageResponse
        {
            Id = resource.Id,
            Name = StringValue(attributes, "name"),
            Channel = StringValue(attributes, "channel"),
            Definition = JsonValue(attributes, "definition"),
            Created = DateValue(attributes, "created"),
            Updated = DateValue(attributes, "updated")
        };
    }

    public static async Task<UniversalContentResponse> GetUniversalContentByTranslationIdAsync(
        KlaviyoClient client,
        string translationId)
    {
        var resource = await GetRelatedResourceAsync(
            client, translationId, TranslationResourceTypes.UniversalContent);
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
