using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Identifiers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Text;

namespace Apps.Klaviyo.Actions;

[ActionList("Universal content")]
public class UniversalContentActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private const string Channel = "email";

    [Action("Search universal content",
        Description = "Searches translations associated with universal content.")]
    public async Task<SearchTranslationsResponse> SearchUniversalContent(
        [ActionParameter] SearchTranslationsRequest input)
    {
        DateRangeValidator.Validate(input.UpdatedFrom, input.UpdatedTo);
        var selectedChannels = TranslationSearchHelper.NormalizeAndValidate(
            input.Channels, TranslationChannels.UniversalContent, "channel");
        string[] resourceTypes = [TranslationResourceTypes.UniversalContent];
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("include", TranslationResourceTypes.UniversalContent);

        var items = new List<TranslationResponse>();
        await foreach (var response in Client.PaginateAsync<TranslationDto>(request))
        {
            var included = response.Included
                .GroupBy(item => $"{item.Type}::{item.Id}")
                .ToDictionary(group => group.Key, group => group.First());
            items.AddRange(response.Data
                .Select(item => TranslationSearchHelper.MapTranslation(item, included))
                .Where(item => TranslationSearchHelper.Matches(
                    item, resourceTypes, selectedChannels, input)));
        }

        return new SearchTranslationsResponse
        {
            Items = items.OrderByDescending(item => item.Updated ?? item.Created ?? DateTime.MinValue).ToList()
        };
    }

    [Action("Get universal content",
        Description = "Gets universal content associated with a translation.")]
    public Task<UniversalContentResponse> GetUniversalContent(
        [ActionParameter] UniversalContentIdentifier input)
    {
        var universalContentId = NormalizeUniversalContentId(input.UniversalContentId);
        return TranslationResourceHelper.GetUniversalContentByTranslationIdAsync(
            Client, $"template-universal-content::email::{universalContentId}");
    }

    [Action("Download universal content", Description = "Downloads source or localized universal content values as HTML, with universal content data as JSON.")]
    public async Task<DownloadUniversalContentResponse> DownloadUniversalContent(
        [ActionParameter] DownloadUniversalContentRequest input)
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

        var locale = TranslationValuesHelper.ResolveDownloadLocale(
            input.Locale, translation.Attributes.TargetLocales, "Universal content", universalContentId);
        var suffix = locale ?? "source";
        var exportedValues = TranslationValuesHelper.SelectExportValues(
            translation.Attributes.Values, locale);
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string>
            {
                [TranslationMetadataKeys.ContentType] = TranslationResourceTypes.UniversalContent,
                [TranslationMetadataKeys.ResourceId] = universalContentId,
                [TranslationMetadataKeys.Channel] = Channel,
                [TranslationMetadataKeys.TranslationId] = translation.Id
            }, exportedValues);
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

    [Action("Upload universal content", Description = "Uploads translated file to the specified existing or new universal content locale.")]
    public async Task UploadUniversalContent(
        [ActionParameter] UploadUniversalContentRequest input)
    {
        var locale = TranslationValuesHelper.ValidateTargetLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        var htmlFile = await TranslationContentReader.ReadAsync(stream, input.Content.Name);
        var (universalContentId, _) = TranslationChannelHelper.ResolveUploadResourceReference(
            input.UniversalContentId,
            htmlFile.Metadata,
            TranslationResourceTypes.UniversalContent,
            TranslationChannels.UniversalContent,
            "Universal content");

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
        TranslationValuesHelper.ValidateValueIds(
            htmlFile.Values, current.Attributes.Values, "Universal content", universalContentId);

        var targetLocales = current.Attributes.TargetLocales.Contains(locale, StringComparer.OrdinalIgnoreCase)
            ? null
            : TranslationValuesHelper.AddTargetLocale(current.Attributes.TargetLocales, locale);
        var request = TranslationRequestBuilder.UpdateValues(
            current.Id, htmlFile.Values, locale, targetLocales);
        await Client.ExecuteWithErrorHandling(request);
    }

    internal static string NormalizeUniversalContentId(string? universalContentId)
    {
        if (string.IsNullOrWhiteSpace(universalContentId))
            throw new PluginMisconfigurationException("Universal content ID is required.");
        var normalized = universalContentId.Trim();
        if (!normalized.Contains("::", StringComparison.Ordinal))
            return normalized;

        var parts = normalized.Split("::", StringSplitOptions.None);
        if (parts.Length == 3 &&
            string.Equals(parts[0], TranslationResourceTypes.UniversalContent, StringComparison.OrdinalIgnoreCase) &&
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
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Returned no universal content '{universalContentId}'.");
    }

    private async Task<TranslationDto?> FindTranslationAsync(string universalContentId)
    {
        var request = TranslationRequestBuilder.FindByResourceId(universalContentId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        return response.Data.FirstOrDefault(item =>
            item.Id.StartsWith($"{TranslationResourceTypes.UniversalContent}::{Channel}::", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Relationships[TranslationResourceTypes.UniversalContent]?["data"]?["id"]?.ToString(),
                universalContentId, StringComparison.Ordinal));
    }

    private async Task<TranslationDto> GetWithValuesAsync(string translationId)
    {
        var request = TranslationRequestBuilder.GetWithValues(translationId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Returned no translation '{translationId}'.");
    }

    private async Task<TranslationDto> CreateTranslationAsync(
        string universalContentId, string locale, string? sourceLocale)
    {
        sourceLocale = TranslationValuesHelper.ValidateSourceLocale(
            sourceLocale, locale, "Universal content");
        var request = TranslationRequestBuilder.Create(
            TranslationResourceTypes.UniversalContent, universalContentId, sourceLocale, locale, Channel);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Klaviyo did not return the created translation for '{universalContentId}'.");
    }

    private async Task<FileReference> SaveAsync(string content, string mediaType, string fileName) =>
        await fileManagementClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(content)), mediaType, fileName);

}
