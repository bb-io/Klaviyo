using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
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
    [Action("Download content", Description = "Downloads selected content as HTML and JSON.")]
    public async Task<DownloadContentResponse> DownloadContent([ActionParameter] DownloadContentRequest input)
    {
        var result = input.ContentType?.Trim().ToLowerInvariant() switch
        {
            TranslationResourceTypes.Template => await DownloadTemplate(input),
            TranslationResourceTypes.CampaignVariation => await DownloadCampaignVariation(input),
            TranslationResourceTypes.FlowMessage => await DownloadFlowMessage(input),
            TranslationResourceTypes.UniversalContent => await DownloadUniversalContent(input),
            _ => throw ExceptionHelper.UnsupportedContentType()
        };

        return result;
    }

    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    [Action("Upload content", Description = "Uploads translated file to the selected content type.")]
    public Task UploadContent([ActionParameter] UploadContentRequest input) =>
        input.ContentType?.Trim().ToLowerInvariant() switch
        {
            TranslationResourceTypes.Template =>
                new TemplateFileService(Client, fileManagementClient).UploadAsync(new UploadTemplateRequest
                {
                    TemplateId = input.ContentId,
                    Locale = input.Locale,
                    SourceLocale = input.SourceLocale,
                    Content = input.Content
                }),
            TranslationResourceTypes.CampaignVariation =>
                new CampaignVariationFileService(Client, fileManagementClient).UploadAsync(new UploadCampaignVariationRequest
                {
                    CampaignVariationId = input.ContentId,
                    Locale = input.Locale,
                    SourceLocale = input.SourceLocale,
                    Content = input.Content
                }),
            TranslationResourceTypes.FlowMessage =>
                new FlowMessageFileService(Client, fileManagementClient).UploadAsync(new UploadFlowMessageRequest
                {
                    FlowMessageId = input.ContentId,
                    Locale = input.Locale,
                    SourceLocale = input.SourceLocale,
                    Content = input.Content
                }),
            TranslationResourceTypes.UniversalContent =>
                new UniversalContentFileService(Client, fileManagementClient).UploadAsync(new UploadUniversalContentRequest
                {
                    UniversalContentId = input.ContentId,
                    Locale = input.Locale,
                    SourceLocale = input.SourceLocale,
                    Content = input.Content
                }),
            _ => throw ExceptionHelper.UnsupportedContentType()
        };

    private async Task<DownloadContentResponse> DownloadTemplate(DownloadContentRequest input)
    {
        var result = await new TemplateFileService(Client, fileManagementClient).DownloadAsync(
            new DownloadTemplateRequest { TemplateId = input.ContentId, Locale = input.Locale });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<DownloadContentResponse> DownloadCampaignVariation(DownloadContentRequest input)
    {
        var result = await new CampaignVariationFileService(Client, fileManagementClient).DownloadAsync(
            new DownloadCampaignVariationRequest
            {
                CampaignVariationId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<DownloadContentResponse> DownloadFlowMessage(DownloadContentRequest input)
    {
        var result = await new FlowMessageFileService(Client, fileManagementClient).DownloadAsync(
            new DownloadFlowMessageRequest { FlowMessageId = input.ContentId, Locale = input.Locale });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<DownloadContentResponse> DownloadUniversalContent(DownloadContentRequest input)
    {
        var result = await new UniversalContentFileService(Client, fileManagementClient).DownloadAsync(
            new DownloadUniversalContentRequest
            {
                UniversalContentId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }
}
