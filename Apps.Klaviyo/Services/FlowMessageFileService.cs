using Apps.Klaviyo.Api;
using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Text;

namespace Apps.Klaviyo.Services;

public class FlowMessageFileService(KlaviyoClient client, IFileManagementClient fileManagementClient)
{
    private const string ResourceType = "flow-message";
    private const string Channel = "email";

    public async Task<DownloadFlowMessageResponse> DownloadAsync(DownloadFlowMessageRequest input)
    {
        var flowMessageId = NormalizeFlowMessageId(input.FlowMessageId);
        var flowMessage = await GetFlowMessageAsync(flowMessageId);
        ValidateEmailFlowMessage(flowMessage);
        var translation = await FindTranslationAsync(flowMessageId)
                          ?? throw new PluginMisconfigurationException(
                              $"Flow message '{flowMessageId}' does not have translations configured yet.");
        translation = await GetWithValuesAsync(translation.Id);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"Flow message '{flowMessageId}' has no exportable translation values.");

        var locale = input.Locale?.Trim();
        var suffix = "source";
        if (!string.IsNullOrWhiteSpace(locale))
        {
            locale = translation.Attributes.TargetLocales.FirstOrDefault(value =>
                         string.Equals(value, locale, StringComparison.OrdinalIgnoreCase))
                     ?? throw new PluginMisconfigurationException(
                         $"Flow message '{flowMessageId}' does not have locale '{locale}'.");
            suffix = locale;
        }

        var exportedValues = translation.Attributes.Values.Select(value =>
        {
            var localizedValue = string.IsNullOrWhiteSpace(locale)
                ? null
                : value.Translations.FirstOrDefault(item =>
                    string.Equals(item.Key, locale, StringComparison.OrdinalIgnoreCase)).Value;
            return new KeyValuePair<string, string>(value.Id,
                string.IsNullOrWhiteSpace(localizedValue) ? value.SourceValue : localizedValue);
        });
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string> { ["FlowMessageId"] = flowMessageId }, exportedValues);
        var fileName = $"{flowMessageId}.{suffix}.html";
        html = TemplateHtmlFilterService.Create(html, fileName);

        var htmlFile = await SaveAsync(html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(flowMessage),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await SaveAsync(json.ToString(), "application/json",
            $"{flowMessageId}.{suffix}.json");

        return new DownloadFlowMessageResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    public async Task UploadAsync(UploadFlowMessageRequest input)
    {
        var locale = ValidateLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        var htmlFile = await TranslationContentReader.ReadAsync(stream, input.Content.Name);
        var flowMessageId = ResolveFlowMessageId(input.FlowMessageId, htmlFile.Metadata);

        var flowMessage = await GetFlowMessageAsync(flowMessageId);
        ValidateEmailFlowMessage(flowMessage);
        var existing = await FindTranslationAsync(flowMessageId);
        TranslationDto translation;
        if (existing is null)
        {
            translation = await CreateTranslationAsync(flowMessageId, locale, input.SourceLocale);
        }
        else
        {
            if (string.Equals(existing.Attributes.SourceLocale, locale, StringComparison.OrdinalIgnoreCase))
                throw new PluginMisconfigurationException("Target locale must differ from source locale.");
            locale = existing.Attributes.TargetLocales.FirstOrDefault(value =>
                         string.Equals(value, locale, StringComparison.OrdinalIgnoreCase)) ?? locale;
            translation = existing;
        }

        var current = await GetWithValuesAsync(translation.Id);
        ValidateValues(htmlFile.Values, current.Attributes.Values, flowMessageId);

        var translatedValues = htmlFile.Values.Select(value => new
        {
            id = value.Key,
            translations = new Dictionary<string, string> { [locale] = value.Value }
        }).ToArray();
        var attributes = new JObject
        {
            ["values"] = JArray.FromObject(translatedValues)
        };
        if (!current.Attributes.TargetLocales.Contains(locale, StringComparer.OrdinalIgnoreCase))
            attributes["target_locales"] = JArray.FromObject(
                AddTargetLocale(current.Attributes.TargetLocales, locale));

        var body = new JObject
        {
            ["data"] = new JObject
            {
                ["type"] = "translation",
                ["id"] = current.Id,
                ["attributes"] = attributes
            }
        };
        var request = new RestRequest("translations/{translationId}", Method.Patch)
            .AddUrlSegment("translationId", current.Id)
            .AddStringBody(body.ToString(Formatting.None), "application/vnd.api+json");
        await client.ExecuteWithErrorHandling(request);
    }

    public static string[] AddTargetLocale(IEnumerable<string> existingLocales, string newLocale) =>
        existingLocales.Append(newLocale).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static string ResolveFlowMessageId(
        string? inputFlowMessageId,
        IReadOnlyDictionary<string, string> metadata)
    {
        var explicitId = string.IsNullOrWhiteSpace(inputFlowMessageId)
            ? null
            : NormalizeFlowMessageId(inputFlowMessageId);
        var metadataId = metadata.TryGetValue("FlowMessageId", out var value) &&
                         !string.IsNullOrWhiteSpace(value)
            ? NormalizeFlowMessageId(value)
            : null;

        if (explicitId is not null && metadataId is not null &&
            !string.Equals(explicitId, metadataId, StringComparison.Ordinal))
            throw new PluginMisconfigurationException(
                $"The HTML file belongs to flow message '{metadataId}', not '{explicitId}'.");

        return explicitId ?? metadataId
            ?? throw new PluginMisconfigurationException(
                "Flow message ID is required either as an input or in the HTML metadata.");
    }

    public static void ValidateValues(
        IReadOnlyDictionary<string, string> uploadedValues,
        IReadOnlyCollection<TranslationValueDto> currentValues,
        string flowMessageId)
    {
        var currentIds = currentValues.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        var unknownIds = uploadedValues.Keys.Where(id => !currentIds.Contains(id)).ToArray();
        if (unknownIds.Length > 0)
            throw new PluginMisconfigurationException(
                $"The HTML file contains value IDs that do not belong to flow message '{flowMessageId}': " +
                string.Join(", ", unknownIds));
    }

    public static string NormalizeFlowMessageId(string? flowMessageId)
    {
        if (string.IsNullOrWhiteSpace(flowMessageId))
            throw new PluginMisconfigurationException("Flow message ID is required.");
        var normalized = flowMessageId.Trim();
        if (!normalized.Contains("::", StringComparison.Ordinal))
            return normalized;

        var parts = normalized.Split("::", StringSplitOptions.None);
        if (parts.Length == 3 &&
            string.Equals(parts[0], ResourceType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(parts[1], Channel, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(parts[2]))
            return parts[2];

        throw new PluginMisconfigurationException(
            "Expected an email flow message ID or flow-message::email:: translation ID.");
    }

    private async Task<RelatedResourceDto> GetFlowMessageAsync(string flowMessageId)
    {
        var request = new RestRequest("flow-messages/{flowMessageId}", Method.Get)
            .AddUrlSegment("flowMessageId", flowMessageId);
        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Klaviyo returned no flow message '{flowMessageId}'.");
    }

    private async Task<TranslationDto?> FindTranslationAsync(string flowMessageId)
    {
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{flowMessageId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        return response.Data.FirstOrDefault(item =>
            item.Id.StartsWith($"{ResourceType}::{Channel}::", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Relationships[ResourceType]?["data"]?["id"]?.ToString(),
                flowMessageId, StringComparison.Ordinal));
    }

    private async Task<TranslationDto> GetWithValuesAsync(string translationId)
    {
        var request = new RestRequest("translations/{translationId}", Method.Get)
            .AddUrlSegment("translationId", translationId)
            .AddQueryParameter("additional-fields[translation]", "values");
        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Klaviyo returned no translation '{translationId}'.");
    }

    private async Task<TranslationDto> CreateTranslationAsync(
        string flowMessageId, string locale, string? sourceLocale)
    {
        if (string.IsNullOrWhiteSpace(sourceLocale))
            throw new PluginMisconfigurationException(
                "Source locale is required when translations have not been configured for this flow message yet.");
        sourceLocale = sourceLocale.Trim();
        if (string.Equals(sourceLocale, locale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Target locale must differ from source locale.");

        var body = new
        {
            data = new
            {
                type = "translation",
                attributes = new
                {
                    source_locale = sourceLocale,
                    target_locales = new[] { locale },
                    fallback_locale = sourceLocale,
                    channel = Channel
                },
                relationships = new Dictionary<string, object>
                {
                    [ResourceType] = new { data = new { type = ResourceType, id = flowMessageId } }
                }
            }
        };
        var request = new RestRequest("translations", Method.Post)
            .AddStringBody(JsonConvert.SerializeObject(body), "application/vnd.api+json");
        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Klaviyo did not return the created translation for '{flowMessageId}'.");
    }

    private static void ValidateEmailFlowMessage(RelatedResourceDto flowMessage)
    {
        var channel = flowMessage.Attributes["channel"]?.ToString();
        if (!string.Equals(channel, Channel, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException(
                $"Flow message '{flowMessage.Id}' must use the email channel.");
    }

    private async Task<FileReference> SaveAsync(string content, string mediaType, string fileName) =>
        await fileManagementClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(content)), mediaType, fileName);

    private static string ValidateLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            throw new PluginMisconfigurationException("Target locale is required.");
        return locale.Trim();
    }
}
