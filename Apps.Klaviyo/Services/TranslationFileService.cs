using System.Text;
using Apps.Klaviyo.Api;
using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Klaviyo.Services;

public class TranslationFileService(KlaviyoClient client, IFileManagementClient fileManagementClient)
{
    public async Task<DownloadTranslationResponse> DownloadAsync(
        DownloadTranslationRequest input, string? expectedResourceType = null)
    {
        ValidateIdentifier(input.ContentId, expectedResourceType);
        var translation = await GetWithValuesAsync(input.ContentId);
        var locale = ValidateLocale(input.TargetLocale, translation.Attributes.TargetLocales);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"Klaviyo translation '{input.ContentId}' has no exportable values.");

        var sourceLocale = translation.Attributes.SourceLocale
            ?? throw new PluginApplicationException("Klaviyo did not return a source locale.");
        var xliff = TranslationFileCodec.Export(input.ContentId, sourceLocale, locale,
            translation.Attributes.Values);
        var resourceId = input.ContentId.Split("::", StringSplitOptions.None).Last();
        var fileName = $"{resourceId}.{locale}.xlf";
        var content = await fileManagementClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(xliff)), "application/xliff+xml", fileName);

        return new DownloadTranslationResponse { Content = content };
    }

    public async Task UploadAsync(UploadTranslationRequest input, string? expectedResourceType = null)
    {
        ValidateIdentifier(input.ContentId, expectedResourceType);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        var translationId = input.ContentId!;
        var translation = await GetWithValuesAsync(translationId);
        var locale = ValidateLocale(input.Locale, translation.Attributes.TargetLocales);
        var currentValues = translation.Attributes.Values.ToDictionary(value => value.Id, StringComparer.Ordinal);

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var xliff = await reader.ReadToEndAsync();
        var sourceLocale = translation.Attributes.SourceLocale
            ?? throw new PluginApplicationException("Klaviyo did not return a source locale.");
        var translatedValues = TranslationFileCodec.Import(xliff, translationId, sourceLocale,
            locale, currentValues);

        var body = new
        {
            data = new
            {
                type = "translation",
                id = translationId,
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
            .AddUrlSegment("translationId", translationId)
            .AddStringBody(JsonConvert.SerializeObject(body), "application/vnd.api+json");
        await client.ExecuteWithErrorHandling(request);
    }

    private async Task<TranslationDto> GetWithValuesAsync(string translationId)
    {
        var request = new RestRequest("translations/{translationId}", Method.Get)
            .AddUrlSegment("translationId", translationId)
            .AddQueryParameter("additional-fields[translation]", "values");
        var response = await client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data
               ?? throw new PluginApplicationException(
                   $"Klaviyo returned no translation for '{translationId}'.");
    }

    private static void ValidateIdentifier(string? translationId, string? expectedResourceType)
    {
        if (string.IsNullOrWhiteSpace(translationId))
            throw new PluginMisconfigurationException("Translation ID is required.");
        if (translationId.Split("::", StringSplitOptions.None).Length != 3)
            throw new PluginMisconfigurationException("Translation ID must be a composite Klaviyo ID.");
        if (expectedResourceType is not null &&
            !translationId.StartsWith($"{expectedResourceType}::", StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException(
                $"Translation ID must refer to resource type '{expectedResourceType}'.");
    }

    private static string ValidateLocale(string? locale, IEnumerable<string> targetLocales)
    {
        if (string.IsNullOrWhiteSpace(locale))
            throw new PluginMisconfigurationException("Target locale is required.");

        var selected = targetLocales.FirstOrDefault(value =>
            string.Equals(value, locale.Trim(), StringComparison.OrdinalIgnoreCase));
        return selected ?? throw new PluginMisconfigurationException(
            $"Locale '{locale}' is not enabled for this translation.");
    }
}
