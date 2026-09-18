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

public class CampaignVariationFileService(KlaviyoClient client, IFileManagementClient fileManagementClient)
{
    private const string ResourceType = "campaign-variation";
    private const string Channel = "email";

    public async Task<DownloadCampaignVariationResponse> DownloadAsync(
        DownloadCampaignVariationRequest input)
    {
        var variationId = NormalizeCampaignVariationId(input.CampaignVariationId);
        var variation = await GetCampaignVariationAsync(variationId);
        ValidateEmailVariation(variation);
        var translation = await FindTranslationAsync(variationId)
                          ?? throw new PluginMisconfigurationException(
                              $"Campaign variation '{variationId}' does not have translations configured yet.");
        translation = await GetWithValuesAsync(translation.Id);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"Campaign variation '{variationId}' has no exportable translation values.");

        var locale = input.Locale?.Trim();
        var suffix = "source";
        if (!string.IsNullOrWhiteSpace(locale))
        {
            locale = translation.Attributes.TargetLocales.FirstOrDefault(value =>
                         string.Equals(value, locale, StringComparison.OrdinalIgnoreCase))
                     ?? throw new PluginMisconfigurationException(
                         $"Campaign variation '{variationId}' does not have locale '{locale}'.");
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
            new Dictionary<string, string> { ["CampaignVariationId"] = variationId }, exportedValues);
        var fileName = $"{variationId}.{suffix}.html";
        html = TemplateHtmlFilterService.Create(html, fileName);

        var htmlFile = await SaveAsync(html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(variation),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await SaveAsync(json.ToString(), "application/json",
            $"{variationId}.{suffix}.json");

        return new DownloadCampaignVariationResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    public async Task UploadAsync(UploadCampaignVariationRequest input)
    {
        var locale = ValidateLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        var extension = Path.GetExtension(input.Content.Name ?? string.Empty).ToLowerInvariant();
        if (extension is not (".html" or ".htm"))
            throw new PluginMisconfigurationException("Upload campaign variation accepts HTML files only.");

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var fileText = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(fileText))
            throw new PluginMisconfigurationException("Content file is empty.");

        var filteredHtml = TemplateHtmlFilterService.Create(
            fileText, input.Content.Name ?? "campaign-variation.html");
        var htmlFile = TranslationHtmlFileCodec.Import(filteredHtml);
        var variationId = ResolveCampaignVariationId(input.CampaignVariationId, htmlFile.Metadata);

        var variation = await GetCampaignVariationAsync(variationId);
        ValidateEmailVariation(variation);
        var existing = await FindTranslationAsync(variationId);
        TranslationDto translation;
        if (existing is null)
        {
            translation = await CreateTranslationAsync(variationId, locale, input.SourceLocale);
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
        ValidateValues(htmlFile.Values, current.Attributes.Values, variationId);

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

    public static string ResolveCampaignVariationId(
        string? inputVariationId,
        IReadOnlyDictionary<string, string> metadata)
    {
        var explicitId = string.IsNullOrWhiteSpace(inputVariationId)
            ? null
            : NormalizeCampaignVariationId(inputVariationId);
        var metadataId = metadata.TryGetValue("CampaignVariationId", out var value) &&
                         !string.IsNullOrWhiteSpace(value)
            ? NormalizeCampaignVariationId(value)
            : null;

        if (explicitId is not null && metadataId is not null &&
            !string.Equals(explicitId, metadataId, StringComparison.Ordinal))
            throw new PluginMisconfigurationException(
                $"The HTML file belongs to campaign variation '{metadataId}', not '{explicitId}'.");

        return explicitId ?? metadataId
            ?? throw new PluginMisconfigurationException(
                "Campaign variation ID is required either as an input or in the HTML metadata.");
    }

    public static void ValidateValues(
        IReadOnlyDictionary<string, string> uploadedValues,
        IReadOnlyCollection<TranslationValueDto> currentValues,
        string variationId)
    {
        var currentById = currentValues.ToDictionary(value => value.Id, StringComparer.Ordinal);
        var unknownIds = uploadedValues.Keys.Where(id => !currentById.ContainsKey(id)).ToArray();
        if (unknownIds.Length > 0)
            throw new PluginMisconfigurationException(
                $"The HTML file contains value IDs that do not belong to campaign variation '{variationId}': " +
                string.Join(", ", unknownIds));

        foreach (var (id, translatedValue) in uploadedValues)
        {
            var sourceFragment = TranslationHtmlFileCodec.ToFragment(currentById[id].SourceValue);
            var sourceDocument = $"<html><head></head><body>{sourceFragment}</body></html>";
            var filteredSource = TemplateHtmlFilterService.Create(sourceDocument, "campaign-variation-value.html");
            filteredSource = TranslationHtmlFileCodec.ToFragment(filteredSource);
            TranslationFileCodec.ValidateHtmlTranslation(filteredSource, translatedValue, id);
        }
    }

    public static string NormalizeCampaignVariationId(string? variationId)
    {
        if (string.IsNullOrWhiteSpace(variationId))
            throw new PluginMisconfigurationException("Campaign variation ID is required.");
        var normalized = variationId.Trim();
        if (!normalized.Contains("::", StringComparison.Ordinal))
            return normalized;

        var parts = normalized.Split("::", StringSplitOptions.None);
        if (parts.Length == 3 &&
            string.Equals(parts[0], ResourceType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(parts[1], Channel, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(parts[2]))
            return parts[2];

        throw new PluginMisconfigurationException(
            "Expected an email campaign variation ID or campaign-variation::email:: translation ID.");
    }

    private async Task<RelatedResourceDto> GetCampaignVariationAsync(string variationId)
    {
        var request = new RestRequest("campaign-messages", Method.Get)
            .AddQueryParameter("include", "campaign-variations")
            .AddQueryParameter("page[size]", "100");
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            var response = await client.ExecuteWithErrorHandling<JsonApiListResponse<RelatedResourceDto>>(request);
            var variation = response.Included.FirstOrDefault(item =>
                string.Equals(item.Type, ResourceType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Id, variationId, StringComparison.Ordinal));
            if (variation is not null)
                return variation;

            var next = response.Links?.Next;
            if (string.IsNullOrWhiteSpace(next) || !visited.Add(next))
                break;
            request = new RestRequest(next, Method.Get);
        }

        throw new PluginApplicationException(
            $"Klaviyo returned no campaign variation '{variationId}'.");
    }

    private async Task<TranslationDto?> FindTranslationAsync(string variationId)
    {
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{variationId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        return response.Data.FirstOrDefault(item =>
            item.Id.StartsWith($"{ResourceType}::{Channel}::", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Relationships[ResourceType]?["data"]?["id"]?.ToString(),
                variationId, StringComparison.Ordinal));
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
        string variationId, string locale, string? sourceLocale)
    {
        if (string.IsNullOrWhiteSpace(sourceLocale))
            throw new PluginMisconfigurationException(
                "Source locale is required when translations have not been configured for this campaign variation yet.");
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
                    [ResourceType] = new { data = new { type = ResourceType, id = variationId } }
                }
            }
        };
        var request = new RestRequest("translations", Method.Post)
            .AddStringBody(JsonConvert.SerializeObject(body), "application/vnd.api+json");
        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Klaviyo did not return the created translation for '{variationId}'.");
    }

    private static void ValidateEmailVariation(RelatedResourceDto variation)
    {
        var channel = variation.Attributes.SelectToken("definition.details.channel")?.ToString()
                      ?? variation.Attributes["channel"]?.ToString();
        if (!string.Equals(channel, Channel, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException(
                $"Campaign variation '{variation.Id}' must use the email channel.");
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
