using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Actions;

[ActionList("Universal content")]
public class UniversalContentActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [Action("Search universal content",
        Description = "Searches translations associated with universal content.")]
    public Task<SearchTranslationsResponse> SearchUniversalContent(
        [ActionParameter] SearchTranslationsRequest input) =>
        new TranslationService(Client).SearchAsync(input, [TranslationResourceTypes.UniversalContent]);

    [Action("Get universal content",
        Description = "Gets universal content associated with a translation.")]
    public Task<UniversalContentResponse> GetUniversalContent(
        [ActionParameter] UniversalContentIdentifier input)
    {
        var universalContentId = UniversalContentFileService.NormalizeUniversalContentId(
            input.UniversalContentId);
        return new TranslationService(Client).GetUniversalContentAsync(
            $"template-universal-content::email::{universalContentId}");
    }

    [Action("Download universal content", Description = "Downloads source or localized universal content values as HTML, with universal content data as JSON.")]
    public Task<DownloadUniversalContentResponse> DownloadUniversalContent(
        [ActionParameter] DownloadUniversalContentRequest input) =>
        new UniversalContentFileService(Client, fileManagementClient).DownloadAsync(input);

    [Action("Upload universal content", Description = "Uploads translated file to the specified existing or new universal content locale.")]
    public Task UploadUniversalContent([ActionParameter] UploadUniversalContentRequest input) =>
        new UniversalContentFileService(Client, fileManagementClient).UploadAsync(input);
}
