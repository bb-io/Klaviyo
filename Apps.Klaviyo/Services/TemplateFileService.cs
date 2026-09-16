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

public class TemplateFileService(KlaviyoClient client, IFileManagementClient fileManagementClient)
{
    public async Task<DownloadTemplateResponse> DownloadAsync(DownloadTemplateRequest input)
    {
        var templateId = ValidateTemplateId(input.TemplateId);
        var template = await GetTemplateAsync(templateId);
        var sourceHtml = template.Attributes["html"]?.ToString();
        if (string.IsNullOrWhiteSpace(sourceHtml))
            throw new PluginApplicationException($"Template '{templateId}' has no exportable HTML.");

        var locale = ValidateLocale(input.Locale);
        var translation = await FindTranslationAsync(templateId);
        locale = translation?.Attributes.TargetLocales.FirstOrDefault(value =>
                     string.Equals(value, locale, StringComparison.OrdinalIgnoreCase)) ?? locale;
        string translationId;
        string sourceLocale;
        IReadOnlyCollection<TranslationValueDto> values;
        if (translation is not null)
        {
            translation = await GetWithValuesAsync(translation.Id);
            translationId = translation.Id;
            sourceLocale = translation.Attributes.SourceLocale
                ?? throw new PluginApplicationException("Klaviyo did not return a source locale.");
            values = translation.Attributes.Values;
            if (values.Count == 0)
                throw new PluginApplicationException($"Template '{templateId}' has no exportable translation values.");
        }
        else
        {
            translationId = $"template::email::{templateId}";
            sourceLocale = ValidateLocale(input.SourceLocale);
            if (string.Equals(sourceLocale, locale, StringComparison.OrdinalIgnoreCase))
                throw new PluginMisconfigurationException("Target locale must differ from source locale.");
            values =
            [
                new TranslationValueDto
                {
                    Id = $"template::{templateId}::body",
                    SourceValue = sourceHtml
                }
            ];
        }
        if (string.Equals(sourceLocale, locale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Target locale must differ from source locale.");

        var xliff = TemplateXliffCodec.Export(translationId, sourceLocale, locale, values);
        var xliffFile = await SaveAsync(xliff, "application/xliff+xml", $"{templateId}.{locale}.xlf");
        var json = new JObject
        {
            ["data"] = JObject.FromObject(template)
        };
        if (translation is not null)
            json["translation"] = JObject.FromObject(translation);
        var jsonFile = await SaveAsync(json.ToString(), "application/json", $"{templateId}.{locale}.json");

        return new DownloadTemplateResponse
        {
            Content = xliffFile,
            JsonFile = jsonFile
        };
    }

    public async Task UploadAsync(UploadTemplateRequest input)
    {
        var templateId = ValidateTemplateId(input.TemplateId);
        var locale = ValidateLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        var extension = Path.GetExtension(input.Content.Name ?? string.Empty).ToLowerInvariant();
        if (extension is not (".xlf" or ".xliff"))
            throw new PluginMisconfigurationException("Upload template accepts XLIFF files only.");

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var fileText = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(fileText))
            throw new PluginMisconfigurationException("Content file is empty.");

        var existing = await FindTranslationAsync(templateId);
        locale = existing?.Attributes.TargetLocales.FirstOrDefault(value =>
                     string.Equals(value, locale, StringComparison.OrdinalIgnoreCase)) ?? locale;
        Dictionary<string, string> translatedValues;
        if (existing is not null)
        {
            var current = await GetWithValuesAsync(existing.Id);
            var sourceLocale = current.Attributes.SourceLocale
                ?? throw new PluginApplicationException("Klaviyo did not return a source locale.");
            translatedValues = TemplateXliffCodec.Import(fileText, input.Content.Name ?? "template.xlf",
                current.Id, sourceLocale, locale,
                current.Attributes.Values.ToDictionary(value => value.Id, StringComparer.Ordinal));
        }
        else
        {
            var sourceLocale = ValidateLocale(input.SourceLocale);
            var template = await GetTemplateAsync(templateId);
            var sourceHtml = template.Attributes["html"]?.ToString();
            if (string.IsNullOrWhiteSpace(sourceHtml))
                throw new PluginApplicationException($"Template '{templateId}' has no exportable HTML.");
            var translationId = $"template::email::{templateId}";
            var provisionalValues = new Dictionary<string, TranslationValueDto>(StringComparer.Ordinal)
            {
                [$"template::{templateId}::body"] = new()
                {
                    Id = $"template::{templateId}::body",
                    SourceValue = sourceHtml
                }
            };
            translatedValues = TemplateXliffCodec.Import(fileText, input.Content.Name ?? "template.xlf",
                translationId, sourceLocale, locale, provisionalValues);
        }

        var translation = await EnsureTranslationAsync(templateId, locale, input.SourceLocale, existing);

        var body = new
        {
            data = new
            {
                type = "translation",
                id = translation.Id,
                attributes = new
                {
                    values = translatedValues.Select(value => new
                    {
                        id = value.Key,
                        translations = new Dictionary<string, string> { [locale] = value.Value }
                    }).ToArray()
                }
            }
        };
        var request = new RestRequest("translations/{translationId}", Method.Patch)
            .AddUrlSegment("translationId", translation.Id)
            .AddStringBody(JsonConvert.SerializeObject(body), "application/vnd.api+json");
        await client.ExecuteWithErrorHandling(request);
    }

    public static string[] AddTargetLocale(IEnumerable<string> existingLocales, string newLocale) =>
        existingLocales.Append(newLocale).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private async Task<RelatedResourceDto> GetTemplateAsync(string templateId)
    {
        var request = new RestRequest("templates/{templateId}", Method.Get)
            .AddUrlSegment("templateId", templateId);
        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response.Data ?? throw new PluginApplicationException($"Klaviyo returned no template '{templateId}'.");
    }

    private async Task<TranslationDto?> FindTranslationAsync(string templateId)
    {
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{templateId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        return response.Data.FirstOrDefault(item =>
            item.Id.StartsWith("template::email::", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Relationships["template"]?["data"]?["id"]?.ToString(),
                templateId, StringComparison.Ordinal));
    }

    private async Task<TranslationDto> GetWithValuesAsync(string translationId)
    {
        var request = new RestRequest("translations/{translationId}", Method.Get)
            .AddUrlSegment("translationId", translationId)
            .AddQueryParameter("additional-fields[translation]", "values");
        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException($"Klaviyo returned no translation '{translationId}'.");
    }

    private async Task<TranslationDto> EnsureTranslationAsync(
        string templateId, string locale, string? sourceLocale, TranslationDto? existing)
    {
        if (existing is null)
        {
            sourceLocale = ValidateLocale(sourceLocale);
            if (string.Equals(sourceLocale, locale, StringComparison.OrdinalIgnoreCase))
                throw new PluginMisconfigurationException("Target locale must differ from source locale.");

            var createBody = new
            {
                data = new
                {
                    type = "translation",
                    attributes = new
                    {
                        source_locale = sourceLocale,
                        target_locales = new[] { locale },
                        fallback_locale = sourceLocale,
                        channel = "email"
                    },
                    relationships = new Dictionary<string, object>
                    {
                        ["template"] = new { data = new { type = "template", id = templateId } }
                    }
                }
            };
            var request = new RestRequest("translations", Method.Post)
                .AddStringBody(JsonConvert.SerializeObject(createBody), "application/vnd.api+json");
            var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
            return response.Data;
        }

        if (existing.Attributes.TargetLocales.Contains(locale, StringComparer.OrdinalIgnoreCase))
            return existing;
        if (string.Equals(existing.Attributes.SourceLocale, locale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Target locale must differ from source locale.");

        var updateBody = new
        {
            data = new
            {
                type = "translation",
                id = existing.Id,
                attributes = new { target_locales = AddTargetLocale(existing.Attributes.TargetLocales, locale) }
            }
        };
        var updateRequest = new RestRequest("translations/{translationId}", Method.Patch)
            .AddUrlSegment("translationId", existing.Id)
            .AddStringBody(JsonConvert.SerializeObject(updateBody), "application/vnd.api+json");
        await client.ExecuteWithErrorHandling(updateRequest);
        return existing;
    }

    private async Task<FileReference> SaveAsync(string content, string mediaType, string fileName) =>
        await fileManagementClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), mediaType, fileName);

    private static string ValidateTemplateId(string? templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            throw new PluginMisconfigurationException("Template ID is required.");
        var normalized = templateId.Trim();
        if (normalized.Contains("::", StringComparison.Ordinal))
        {
            var parts = normalized.Split("::", StringSplitOptions.None);
            if (parts.Length != 3 ||
                !string.Equals(parts[0], "template", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(parts[1], "email", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(parts[2]))
                throw new PluginMisconfigurationException(
                    "Expected an email template ID or template::email:: translation ID.");
            return parts[2];
        }
        return normalized;
    }

    private static string ValidateLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            throw new PluginMisconfigurationException("Target locale is required.");
        return locale.Trim();
    }
}
