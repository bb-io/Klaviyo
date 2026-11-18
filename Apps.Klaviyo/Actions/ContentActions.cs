using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Actions;

[ActionList("Content")]
public class ContentActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [BlueprintActionDefinition(BlueprintAction.SearchContent)]
    [Action("Search content", Description = "Searches all content with translations.")]
    public Task<SearchTranslationsResponse> SearchContent([ActionParameter] SearchContentRequest input)
    {
        return TranslationSearchHelper.SearchAsync(
            Client,
            new SearchTranslationsRequest
            {
                Channels = input.Channels,
                UpdatedFrom = input.UpdatedFrom,
                UpdatedTo = input.UpdatedTo
            },
            input.ContentTypes,
            TranslationChannels.All);
    }

    [BlueprintActionDefinition(BlueprintAction.DownloadContent)]
    [Action("Download content", Description = "Downloads selected content as HTML and JSON.")]
    public async Task<DownloadContentResponse> DownloadContent([ActionParameter] DownloadContentRequest input)
    {
        var contentType = input.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;
        TranslationChannelHelper.NormalizeOptionalChannel(
            input.Channel, TranslationChannels.ForResourceType(contentType),
            TranslationResourceDisplayNames.Content);
        return contentType switch
        {
            TranslationResourceTypes.Template => await DownloadTemplate(input),
            TranslationResourceTypes.CampaignVariation => await DownloadCampaignVariation(input),
            TranslationResourceTypes.FlowMessage => await DownloadFlowMessage(input),
            TranslationResourceTypes.UniversalContent => await DownloadUniversalContent(input),
            _ => throw ExceptionHelper.UnsupportedContentType()
        };
    }

    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    [Action("Upload content", Description = "Uploads translated content, inferring its type from file metadata when omitted.")]
    public async Task UploadContent([ActionParameter] UploadContentRequest input)
    {
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        var htmlFile = await TranslationFileService.ReadAsync(fileManagementClient, input.Content);
        var contentType = ResolveUploadContentType(input.ContentType, htmlFile.Metadata);

        var upload = contentType switch
        {
            TranslationResourceTypes.Template =>
                new TemplateActions(InvocationContext, fileManagementClient).UploadTemplate(
                    new UploadTemplateRequest
                    {
                        TemplateId = input.ContentId,
                        Locale = input.Locale,
                        SourceLocale = input.SourceLocale,
                        Content = input.Content
                    },
                    htmlFile),
            TranslationResourceTypes.CampaignVariation =>
                new CampaignVariationActions(InvocationContext, fileManagementClient).UploadCampaignVariation(
                    new UploadCampaignVariationRequest
                    {
                        CampaignVariationId = input.ContentId,
                        Locale = input.Locale,
                        SourceLocale = input.SourceLocale,
                        Content = input.Content
                    },
                    htmlFile),
            TranslationResourceTypes.FlowMessage =>
                new FlowMessageActions(InvocationContext, fileManagementClient).UploadFlowMessage(
                    new UploadFlowMessageRequest
                    {
                        FlowMessageId = input.ContentId,
                        Locale = input.Locale,
                        SourceLocale = input.SourceLocale,
                        Content = input.Content
                    },
                    htmlFile),
            TranslationResourceTypes.UniversalContent =>
                new UniversalContentActions(InvocationContext, fileManagementClient).UploadUniversalContent(
                    new UploadUniversalContentRequest
                    {
                        UniversalContentId = input.ContentId,
                        Locale = input.Locale,
                        SourceLocale = input.SourceLocale,
                        Content = input.Content
                    },
                    htmlFile),
            _ => throw ExceptionHelper.UnsupportedContentType()
        };

        await upload;
    }

    private static string ResolveUploadContentType(
        string? inputContentType,
        IReadOnlyDictionary<string, string> metadata)
    {
        var inputType = NormalizeOptionalContentType(inputContentType);
        var metadataType = NormalizeOptionalContentType(
            metadata.GetValueOrDefault(TranslationMetadataKeys.ContentType));

        if (inputType is not null && metadataType is not null && inputType != metadataType)
            throw new PluginMisconfigurationException(
                $"The content file contains content type '{metadataType}', not '{inputType}'.");

        return inputType ?? metadataType
            ?? throw new PluginMisconfigurationException(
                "Content type is required either as an input or in the content file metadata.");
    }

    private static string? NormalizeOptionalContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return null;

        var normalized = contentType.Trim().ToLowerInvariant();
        return TranslationResourceTypes.All.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? normalized
            : throw ExceptionHelper.UnsupportedContentType();
    }

    private async Task<DownloadContentResponse> DownloadTemplate(DownloadContentRequest input)
    {
        var result = await new TemplateActions(InvocationContext, fileManagementClient).DownloadTemplate(
            new DownloadTemplateRequest
            {
                Channel = input.Channel,
                TemplateId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<DownloadContentResponse> DownloadCampaignVariation(DownloadContentRequest input)
    {
        var result = await new CampaignVariationActions(InvocationContext, fileManagementClient).DownloadCampaignVariation(
            new DownloadCampaignVariationRequest
            {
                Channel = input.Channel,
                CampaignVariationId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<DownloadContentResponse> DownloadFlowMessage(DownloadContentRequest input)
    {
        var result = await new FlowMessageActions(InvocationContext, fileManagementClient).DownloadFlowMessage(
            new DownloadFlowMessageRequest
            {
                Channel = input.Channel,
                FlowMessageId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

    private async Task<DownloadContentResponse> DownloadUniversalContent(DownloadContentRequest input)
    {
        var result = await new UniversalContentActions(InvocationContext, fileManagementClient).DownloadUniversalContent(
            new DownloadUniversalContentRequest
            {
                UniversalContentId = input.ContentId,
                Locale = input.Locale
            });
        return new DownloadContentResponse { Content = result.Content, JsonFile = result.JsonFile };
    }

}
