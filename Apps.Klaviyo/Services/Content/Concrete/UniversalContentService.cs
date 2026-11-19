using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Services.Content.Models;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.Klaviyo.Services.Content.Concrete;

public class UniversalContentService(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient)
    : BaseContentService(invocationContext, fileManagementClient)
{
    private const string Channel = TranslationChannels.Email;

    public override async Task<ContentDownloadResult> DownloadAsync(ContentDownloadServiceRequest request)
    {
        var universalContentId = NormalizeUniversalContentId(request.ContentId);
        var universalContent = await GetUniversalContentAsync(universalContentId);

        return await DownloadTranslatedResourceAsync(
            new ContentDownloadServiceRequest
            {
                ContentId = universalContentId,
                Channel = Channel,
                Locale = request.Locale
            },
            TranslationResourceTypes.UniversalContent,
            TranslationResourceDisplayNames.UniversalContent,
            TranslationChannels.UniversalContent,
            Channel,
            universalContent);
    }

    public override async Task UploadAsync(ContentUploadServiceRequest request)
    {
        var (locale, content) = await PrepareUploadAsync(request);
        var (universalContentId, _) = TranslationChannelHelper.ResolveUploadResourceReference(
            request.ContentId,
            content.Metadata,
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
                request.SourceLocale,
                content.Values));
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
            string.Equals(
                parts[0],
                TranslationResourceTypes.UniversalContent,
                StringComparison.OrdinalIgnoreCase) &&
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
        return response.Data
               ?? throw new PluginApplicationException(
                   $"Returned no universal content '{universalContentId}'.");
    }
}
