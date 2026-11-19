using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Identifiers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services.Content;
using Apps.Klaviyo.Services.Content.Concrete;
using Apps.Klaviyo.Services.Content.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Actions;

[ActionList("Universal content")]
public class UniversalContentActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private readonly IContentService _contentService =
        new ContentServiceFactory(invocationContext, fileManagementClient)
            .GetContentService(TranslationResourceTypes.UniversalContent);

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
        var universalContentId = UniversalContentService.NormalizeUniversalContentId(input.UniversalContentId);
        return TranslationResourceHelper.GetUniversalContentByTranslationIdAsync(
            Client, $"template-universal-content::email::{universalContentId}");
    }

    [Action("Download universal content", Description = "Downloads source or localized universal content values as HTML, with universal content data as JSON.")]
    public async Task<DownloadUniversalContentResponse> DownloadUniversalContent(
        [ActionParameter] DownloadUniversalContentRequest input)
    {
        var result = await _contentService.DownloadAsync(new ContentDownloadServiceRequest
        {
            ContentId = input.UniversalContentId,
            Locale = input.Locale
        });

        return new DownloadUniversalContentResponse
        {
            Content = result.Content,
            JsonFile = result.JsonFile
        };
    }

    [Action("Upload universal content", Description = "Uploads translated file to the specified existing or new universal content locale.")]
    public Task UploadUniversalContent(
        [ActionParameter] UploadUniversalContentRequest input) =>
        _contentService.UploadAsync(new ContentUploadServiceRequest
        {
            ContentId = input.UniversalContentId,
            Locale = input.Locale,
            SourceLocale = input.SourceLocale,
            Content = input.Content
        });

}
