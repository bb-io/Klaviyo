using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Identifiers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services.Content;
using Apps.Klaviyo.Services.Content.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Actions;

[ActionList("Templates")]
public class TemplateActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private static readonly string[] SupportedChannels = TranslationChannels.Template;
    private readonly IContentService _contentService =
        new ContentServiceFactory(invocationContext, fileManagementClient)
            .GetContentService(TranslationResourceTypes.Template);

    [Action("Search templates", Description = "Searches translations associated with templates.")]
    public Task<SearchTranslationsResponse> SearchTemplates(
        [ActionParameter] SearchTranslationsRequest input)
    {
        return TranslationSearchHelper.SearchAsync(
            Client, input, [TranslationResourceTypes.Template], SupportedChannels);
    }

    [Action("Get template", Description = "Gets a template associated with a translation.")]
    public async Task<TemplateResponse> GetTemplate([ActionParameter] TemplateIdentifier input)
    {
        var (templateId, idChannel) = ParseTemplateId(input.TemplateId);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.Template, SupportedChannels, input.Channel, idChannel);
        var translation = await TranslationResourceHelper.FindTranslationAsync(
                              Client, TranslationResourceTypes.Template, templateId,
                              SupportedChannels, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"Template '{templateId}' does not have translations configured" +
                              (requestedChannel is null ? "." : $" for channel '{requestedChannel}'."));
        return await TranslationResourceHelper.GetTemplateByTranslationIdAsync(Client, translation.Id);
    }

    [Action("Download template", Description = "Downloads the source template or an optional locale as HTML, with template data as JSON.")]
    public async Task<DownloadTemplateResponse> DownloadTemplate(
        [ActionParameter] DownloadTemplateRequest input)
    {
        var result = await _contentService.DownloadAsync(new ContentDownloadServiceRequest
        {
            ContentId = input.TemplateId,
            Channel = input.Channel,
            Locale = input.Locale
        });

        return new DownloadTemplateResponse
        {
            Content = result.Content,
            JsonFile = result.JsonFile
        };
    }

    [Action("Upload template", Description = "Uploads translated file to the specified existing or new template locale.")]
    public Task UploadTemplate([ActionParameter] UploadTemplateRequest input) =>
        _contentService.UploadAsync(new ContentUploadServiceRequest
        {
            ContentId = input.TemplateId,
            Locale = input.Locale,
            SourceLocale = input.SourceLocale,
            Content = input.Content
        });

    private static (string ResourceId, string? Channel) ParseTemplateId(string? templateId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            templateId, TranslationResourceTypes.Template, SupportedChannels,
            TranslationResourceDisplayNames.Template);

}
