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
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Klaviyo.Actions;

[ActionList("Universal content")]
public class UniversalContentActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private const string Channel = "email";

    [Action("Search universal content",
        Description = "Searches translations associated with universal content.")]
    public Task<SearchTranslationsResponse> SearchUniversalContent(
        [ActionParameter] SearchTranslationsRequest input)
    {
        return TranslationSearchHelper.SearchAsync(
            Client, input, [TranslationResourceTypes.UniversalContent], TranslationChannels.UniversalContent);
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
        var translation = await TranslationResourceHelper.FindTranslationAsync(
                              Client, TranslationResourceTypes.UniversalContent, universalContentId,
                              TranslationChannels.UniversalContent, Channel)
                          ?? throw new PluginMisconfigurationException(
                              $"Universal content '{universalContentId}' does not have translations configured yet.");
        translation = await TranslationResourceHelper.GetWithValuesAsync(Client, translation.Id);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"Universal content '{universalContentId}' has no exportable translation values.");

        var locale = TranslationValuesHelper.ResolveDownloadLocale(
            input.Locale, translation.Attributes.TargetLocales,
            TranslationResourceDisplayNames.UniversalContent, universalContentId);
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

        var htmlFile = await TranslationFileService.UploadTextAsync(
            fileManagementClient, html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(universalContent),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await TranslationFileService.UploadTextAsync(fileManagementClient,
            json.ToString(), "application/json",
            $"{universalContentId}.{suffix}.json");

        return new DownloadUniversalContentResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    [Action("Upload universal content", Description = "Uploads translated file to the specified existing or new universal content locale.")]
    public Task UploadUniversalContent(
        [ActionParameter] UploadUniversalContentRequest input) =>
        UploadUniversalContent(input, null);

    internal async Task UploadUniversalContent(
        UploadUniversalContentRequest input,
        TranslationHtmlFile? htmlFile)
    {
        var locale = TranslationValuesHelper.ValidateTargetLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        htmlFile ??= await TranslationFileService.ReadAsync(fileManagementClient, input.Content);
        var (universalContentId, _) = TranslationChannelHelper.ResolveUploadResourceReference(
            input.UniversalContentId,
            htmlFile.Metadata,
            TranslationResourceTypes.UniversalContent,
            TranslationChannels.UniversalContent,
            TranslationResourceDisplayNames.UniversalContent);

        await GetUniversalContentAsync(universalContentId);
        await TranslationUploadService.UploadAsync(
            Client,
            new TranslationUploadContext(
                TranslationResourceTypes.UniversalContent,
                TranslationResourceDisplayNames.UniversalContent,
                universalContentId,
                TranslationChannels.UniversalContent,
                Channel,
                locale,
                input.SourceLocale,
                htmlFile.Values));
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

}
