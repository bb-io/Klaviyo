using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Actions;

[ActionList("Templates")]
public class TemplateActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [Action("Search templates", Description = "Searches translations associated with templates.")]
    public Task<SearchTranslationsResponse> SearchTemplates(
        [ActionParameter] SearchTranslationsRequest input) =>
        new TranslationService(Client).SearchAsync(input, [TranslationResourceTypes.Template]);

    [Action("Get template", Description = "Gets a template associated with a translation.")]
    public Task<TemplateResponse> GetTemplate([ActionParameter] TranslationIdentifier input) =>
        new TranslationService(Client).GetTemplateAsync(input.TranslationId);

    [Action("Download template", Description = "Downloads a template translation as XLIFF 2.0.")]
    public Task<DownloadTranslationResponse> DownloadTemplate([ActionParameter] DownloadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).DownloadAsync(input, TranslationResourceTypes.Template);

    [Action("Upload template", Description = "Uploads translated XLIFF values to a template translation.")]
    public Task UploadTemplate([ActionParameter] UploadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).UploadAsync(input, TranslationResourceTypes.Template);
}
