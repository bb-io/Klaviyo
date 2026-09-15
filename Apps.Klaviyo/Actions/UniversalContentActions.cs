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
        [ActionParameter] TranslationIdentifier input) =>
        new TranslationService(Client).GetUniversalContentAsync(input.TranslationId);

    [Action("Download universal content", Description = "Downloads universal content translation as XLIFF 2.0.")]
    public Task<DownloadTranslationResponse> DownloadUniversalContent([ActionParameter] DownloadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).DownloadAsync(input, TranslationResourceTypes.UniversalContent);

    [Action("Upload universal content", Description = "Uploads translated XLIFF values to universal content.")]
    public Task UploadUniversalContent([ActionParameter] UploadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).UploadAsync(input, TranslationResourceTypes.UniversalContent);
}
