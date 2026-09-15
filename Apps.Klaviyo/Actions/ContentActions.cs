using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Actions;

[ActionList("Content")]
public class ContentActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [BlueprintActionDefinition(BlueprintAction.SearchContent)]
    [Action("Search content", Description = "Searches all content with translations.")]
    public Task<SearchTranslationsResponse> SearchContent([ActionParameter] SearchContentRequest input) =>
        new TranslationService(Client).SearchAsync(input, input.ContentTypes);

    [BlueprintActionDefinition(BlueprintAction.DownloadContent)]
    [Action("Download content", Description = "Downloads Klaviyo translation values as XLIFF 2.0.")]
    public Task<DownloadTranslationResponse> DownloadContent([ActionParameter] DownloadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).DownloadAsync(input);

    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    [Action("Upload content", Description = "Uploads translated XLIFF values to Klaviyo.")]
    public Task UploadContent([ActionParameter] UploadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).UploadAsync(input);
}
