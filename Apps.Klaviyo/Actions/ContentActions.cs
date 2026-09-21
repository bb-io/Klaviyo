using Apps.Klaviyo.Api;
using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Klaviyo.Actions;

[ActionList("Content")]
public class ContentActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [BlueprintActionDefinition(BlueprintAction.SearchContent)]
    [Action("Search content", Description = "Searches all content with translations.")]
    public Task<SearchTranslationsResponse> SearchContent([ActionParameter] SearchContentRequest input)
    {
        DateRangeValidator.Validate(input.UpdatedFrom, input.UpdatedTo);
        return SearchAsync(new SearchTranslationsRequest
        {
            Channels = input.Channels,
            UpdatedFrom = input.UpdatedFrom,
            UpdatedTo = input.UpdatedTo
        }, input.ContentTypes);
    }

    [BlueprintActionDefinition(BlueprintAction.DownloadContent)]
    [Action("Download content", Description = "Downloads selected content as HTML and JSON.")]
    public async Task<DownloadContentResponse> DownloadContent([ActionParameter] DownloadContentRequest input)
    {
        var contentType = input.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;
        TranslationChannelHelper.NormalizeOptionalChannel(
            input.Channel, TranslationChannels.ForResourceType(contentType), "Content");
        return contentType switch
        {
            TranslationResourceTypes.Template => await DownloadTemplate(input),
            TranslationResourceTypes.CampaignVariation => await DownloadCampaignVariation(input),
            TranslationResourceTypes.FlowMessage => await DownloadFlowMessage(input),
            TranslationResourceTypes.UniversalContent => await DownloadUniversalContent(input),
            _ => throw ExceptionHelper.UnsupportedContentType()
        };
    }

    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    [Action("Upload content", Description = "Uploads translated content, inferring its type from file metadata when omitted.")]
    public async Task UploadContent([ActionParameter] UploadContentRequest input)
    {
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        var htmlFile = await TranslationContentReader.ReadAsync(stream, input.Content.Name);
        var contentType = ResolveUploadContentType(input.ContentType, htmlFile.Metadata);

        var upload = contentType switch
        {
            TranslationResourceTypes.Template =>
                new TemplateActions(InvocationContext, fileManagementClient).UploadTemplate(new UploadTemplateRequest
                {
                    TemplateId = input.ContentId,
                    Locale = input.Locale,
                    SourceLocale = input.SourceLocale,
                    Content = input.Content
                }),
            TranslationResourceTypes.CampaignVariation =>
                new CampaignVariationActions(InvocationContext, fileManagementClient).UploadCampaignVariation(new UploadCampaignVariationRequest
                {
                    CampaignVariationId = input.ContentId,
                    Locale = input.Locale,
                    SourceLocale = input.SourceLocale,
                    Content = input.Content
                }),
            TranslationResourceTypes.FlowMessage =>
                new FlowMessageActions(InvocationContext, fileManagementClient).UploadFlowMessage(new UploadFlowMessageRequest
                {
                    FlowMessageId = input.ContentId,
                    Locale = input.Locale,
                    SourceLocale = input.SourceLocale,
                    Content = input.Content
                }),
            TranslationResourceTypes.UniversalContent =>
                new UniversalContentActions(InvocationContext, fileManagementClient).UploadUniversalContent(new UploadUniversalContentRequest
                {
                    UniversalContentId = input.ContentId,
                    Locale = input.Locale,
                    SourceLocale = input.SourceLocale,
                    Content = input.Content
                }),
            _ => throw ExceptionHelper.UnsupportedContentType()
        };

        await upload;
    }

    private static string ResolveUploadContentType(
        string? inputContentType,
        IReadOnlyDictionary<string, string> metadata)
    {
        var inputType = NormalizeOptionalContentType(inputContentType);
        var metadataType = NormalizeOptionalContentType(metadata.GetValueOrDefault("ContentType"));

        if (inputType is not null && metadataType is not null && inputType != metadataType)
            throw new PluginMisconfigurationException(
                $"The content file contains content type '{metadataType}', not '{inputType}'.");

        return inputType ?? metadataType
            ?? throw new PluginMisconfigurationException(
                "Content type is required either as an input or in the content file metadata.");
    }

    private static string? NormalizeOptionalContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return null;

        var normalized = contentType.Trim().ToLowerInvariant();
        return TranslationResourceTypes.All.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? normalized
            : throw ExceptionHelper.UnsupportedContentType();
    }

    private async Task<DownloadContentResponse> DownloadTemplate(DownloadContentRequest input)
    {
        var result = await new TemplateActions(InvocationContext, fileManagementClient).DownloadTemplate(
            new DownloadTemplateRequest
            {
                Channel = input.Channel,
                TemplateId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<DownloadContentResponse> DownloadCampaignVariation(DownloadContentRequest input)
    {
        var result = await new CampaignVariationActions(InvocationContext, fileManagementClient).DownloadCampaignVariation(
            new DownloadCampaignVariationRequest
            {
                Channel = input.Channel,
                CampaignVariationId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<DownloadContentResponse> DownloadFlowMessage(DownloadContentRequest input)
    {
        var result = await new FlowMessageActions(InvocationContext, fileManagementClient).DownloadFlowMessage(
            new DownloadFlowMessageRequest
            {
                Channel = input.Channel,
                FlowMessageId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<DownloadContentResponse> DownloadUniversalContent(DownloadContentRequest input)
    {
        var result = await new UniversalContentActions(InvocationContext, fileManagementClient).DownloadUniversalContent(
            new DownloadUniversalContentRequest
            {
                UniversalContentId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<SearchTranslationsResponse> SearchAsync(
        SearchTranslationsRequest input,
        IEnumerable<string>? resourceTypes = null)
    {
        var selectedChannels = TranslationSearchHelper.NormalizeAndValidate(
            input.Channels,
            TranslationChannels.All,
            "channel");
        var requestedResourceTypes = resourceTypes?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        var selectedResourceTypes = TranslationSearchHelper.NormalizeAndValidate(
            requestedResourceTypes is { Length: > 0 } ? requestedResourceTypes : TranslationResourceTypes.All,
            TranslationResourceTypes.All,
            "content type");

        var request = new RestRequest("translations", Method.Get);

        // Klaviyo currently returns an empty collection for some documented
        // resource_type filters. Fetch the collection and apply the same
        // resource-type restriction locally for these resource types.
        if (selectedResourceTypes.Count == 1 &&
            !selectedResourceTypes[0].Equals(TranslationResourceTypes.CampaignVariation,
                StringComparison.OrdinalIgnoreCase) &&
            !selectedResourceTypes[0].Equals(TranslationResourceTypes.FlowMessage,
                StringComparison.OrdinalIgnoreCase) &&
            !selectedResourceTypes[0].Equals(TranslationResourceTypes.UniversalContent,
                StringComparison.OrdinalIgnoreCase))
            request.AddQueryParameter("filter", $"equals(resource_type,\"{selectedResourceTypes[0]}\")");

        request.AddQueryParameter("include", string.Join(',', selectedResourceTypes));

        var items = new List<TranslationResponse>();
        await foreach (var response in Client.PaginateAsync<TranslationDto>(request))
        {
            var included = response.Included
                .GroupBy(item => $"{item.Type}::{item.Id}")
                .ToDictionary(group => group.Key, group => group.First());

            items.AddRange(response.Data
                .Select(item => TranslationSearchHelper.MapTranslation(item, included))
                .Where(item => TranslationSearchHelper.Matches(
                    item, selectedResourceTypes, selectedChannels, input)));
        }

        return new SearchTranslationsResponse
        {
            Items = items
                .OrderByDescending(item => item.Updated ?? item.Created ?? DateTime.MinValue)
                .ToList()
        };
    }

    internal static async Task<RelatedResourceDto> GetRelatedResourceAsync(
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

    internal static async Task<TemplateResponse> GetTemplateAsync(
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

    internal static async Task<CampaignVariationResponse> GetCampaignVariationAsync(
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

    internal static async Task<FlowMessageResponse> GetFlowMessageAsync(
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

    internal static async Task<UniversalContentResponse> GetUniversalContentAsync(
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
