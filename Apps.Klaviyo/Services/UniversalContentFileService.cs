using System.Text;
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

namespace Apps.Klaviyo.Services;

public class UniversalContentFileService(KlaviyoClient client, IFileManagementClient fileManagementClient)
{
    private const string ResourceType = "template-universal-content";
    private const string Channel = "email";

    public async Task<DownloadUniversalContentResponse> DownloadAsync(
        DownloadUniversalContentRequest input)
    {
        var universalContentId = NormalizeUniversalContentId(input.UniversalContentId);
        var universalContent = await GetUniversalContentAsync(universalContentId);
        var translation = await FindTranslationAsync(universalContentId)
                          ?? throw new PluginMisconfigurationException(
                              $"Universal content '{universalContentId}' does not have translations configured yet.");
        translation = await GetWithValuesAsync(translation.Id);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"Universal content '{universalContentId}' has no exportable translation values.");

        var locale = input.Locale?.Trim();
        var suffix = "source";
        if (!string.IsNullOrWhiteSpace(locale))
        {
            locale = translation.Attributes.TargetLocales.FirstOrDefault(value =>
                         string.Equals(value, locale, StringComparison.OrdinalIgnoreCase))
                     ?? throw new PluginMisconfigurationException(
                         $"Universal content '{universalContentId}' does not have locale '{locale}'.");
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
            new Dictionary<string, string> { ["UniversalContentId"] = universalContentId }, exportedValues);
        var fileName = $"{universalContentId}.{suffix}.html";
        html = TemplateHtmlFilterService.Create(html, fileName);

        var htmlFile = await SaveAsync(html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(universalContent),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await SaveAsync(json.ToString(), "application/json",
            $"{universalContentId}.{suffix}.json");

        return new DownloadUniversalContentResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    public async Task UploadAsync(UploadUniversalContentRequest input)
    {
        var locale = ValidateLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        var extension = Path.GetExtension(input.Content.Name ?? string.Empty).ToLowerInvariant();
        if (extension is not (".html" or ".htm"))
            throw new PluginMisconfigurationException("Upload universal content accepts HTML files only.");

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var fileText = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(fileText))
            throw new PluginMisconfigurationException("Content file is empty.");

        var filteredHtml = TemplateHtmlFilterService.Create(
            fileText, input.Content.Name ?? "universal-content.html");
        var htmlFile = TranslationHtmlFileCodec.Import(filteredHtml);
        var universalContentId = ResolveUniversalContentId(input.UniversalContentId, htmlFile.Metadata);

        await GetUniversalContentAsync(universalContentId);
        var existing = await FindTranslationAsync(universalContentId);
        TranslationDto translation;
        if (existing is null)
        {
            translation = await CreateTranslationAsync(universalContentId, locale, input.SourceLocale);
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
        ValidateValues(htmlFile.Values, current.Attributes.Values, universalContentId);

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

    public static string ResolveUniversalContentId(
        string? inputUniversalContentId,
        IReadOnlyDictionary<string, string> metadata)
    {
        var explicitId = string.IsNullOrWhiteSpace(inputUniversalContentId)
            ? null
            : NormalizeUniversalContentId(inputUniversalContentId);
        var metadataId = metadata.TryGetValue("UniversalContentId", out var value) &&
                         !string.IsNullOrWhiteSpace(value)
            ? NormalizeUniversalContentId(value)
            : null;

        if (explicitId is not null && metadataId is not null &&
            !string.Equals(explicitId, metadataId, StringComparison.Ordinal))
            throw new PluginMisconfigurationException(
                $"The HTML file belongs to universal content '{metadataId}', not '{explicitId}'.");

        return explicitId ?? metadataId
            ?? throw new PluginMisconfigurationException(
                "Universal content ID is required either as an input or in the HTML metadata.");
    }

    public static void ValidateValues(
        IReadOnlyDictionary<string, string> uploadedValues,
        IReadOnlyCollection<TranslationValueDto> currentValues,
        string universalContentId)
    {
        var currentById = currentValues.ToDictionary(value => value.Id, StringComparer.Ordinal);
        var unknownIds = uploadedValues.Keys.Where(id => !currentById.ContainsKey(id)).ToArray();
        if (unknownIds.Length > 0)
            throw new PluginMisconfigurationException(
                $"The HTML file contains value IDs that do not belong to universal content '{universalContentId}': " +
                string.Join(", ", unknownIds));

        foreach (var (id, translatedValue) in uploadedValues)
        {
            var sourceFragment = TranslationHtmlFileCodec.ToFragment(currentById[id].SourceValue);
            var sourceDocument = $"<html><head></head><body>{sourceFragment}</body></html>";
            var filteredSource = TemplateHtmlFilterService.Create(sourceDocument, "universal-content-value.html");
            filteredSource = TranslationHtmlFileCodec.ToFragment(filteredSource);
            TranslationFileCodec.ValidateHtmlTranslation(filteredSource, translatedValue, id);
        }
    }

    public static string NormalizeUniversalContentId(string? universalContentId)
    {
        if (string.IsNullOrWhiteSpace(universalContentId))
            throw new PluginMisconfigurationException("Universal content ID is required.");
        var normalized = universalContentId.Trim();
        if (!normalized.Contains("::", StringComparison.Ordinal))
            return normalized;

        var parts = normalized.Split("::", StringSplitOptions.None);
        if (parts.Length == 3 &&
            string.Equals(parts[0], ResourceType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(parts[1], Channel, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(parts[2]))
            return parts[2];

        throw new PluginMisconfigurationException(
            "Expected an email universal content ID or template-universal-content::email:: translation ID.");
    }

    private async Task<RelatedResourceDto> GetUniversalContentAsync(string universalContentId)
    {
        var request = new RestRequest("template-universal-content/{universalContentId}", Method.Get)
            .AddUrlSegment("universalContentId", universalContentId);
        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Klaviyo returned no universal content '{universalContentId}'.");
    }

    private async Task<TranslationDto?> FindTranslationAsync(string universalContentId)
    {
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{universalContentId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        return response.Data.FirstOrDefault(item =>
            item.Id.StartsWith($"{ResourceType}::{Channel}::", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Relationships[ResourceType]?["data"]?["id"]?.ToString(),
                universalContentId, StringComparison.Ordinal));
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
        string universalContentId, string locale, string? sourceLocale)
    {
        if (string.IsNullOrWhiteSpace(sourceLocale))
            throw new PluginMisconfigurationException(
                "Source locale is required when translations have not been configured for this universal content yet.");
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
                    [ResourceType] = new { data = new { type = ResourceType, id = universalContentId } }
                }
            }
        };
        var request = new RestRequest("translations", Method.Post)
            .AddStringBody(JsonConvert.SerializeObject(body), "application/vnd.api+json");
        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data;
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
