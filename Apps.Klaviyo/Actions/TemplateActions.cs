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

    [Action("Download template", Description = "Downloads template translation values as XLIFF and template data as JSON.")]
    public Task<DownloadTemplateResponse> DownloadTemplate([ActionParameter] DownloadTemplateRequest input) =>
        new TemplateFileService(Client, fileManagementClient).DownloadAsync(input);

    [Action("Upload template", Description = "Uploads translated XLIFF to the specified existing or new template locale.")]
    public Task UploadTemplate([ActionParameter] UploadTemplateRequest input) =>
        new TemplateFileService(Client, fileManagementClient).UploadAsync(input);
}
