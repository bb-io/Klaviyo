using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Apps.Klaviyo.Services.Content;
using Apps.Klaviyo.Services.Content.Models;
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
    private readonly ContentServiceFactory _contentServiceFactory =
        new(invocationContext, fileManagementClient);

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

        var service = _contentServiceFactory.GetContentService(contentType);
        var result = await service.DownloadAsync(new ContentDownloadServiceRequest
        {
            ContentId = input.ContentId,
            Channel = input.Channel,
            Locale = input.Locale
        });
        return new DownloadContentResponse
        {
            Content = result.Content,
            JsonFile = result.JsonFile
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
        var service = _contentServiceFactory.GetContentService(contentType);
        await service.UploadAsync(new ContentUploadServiceRequest
        {
            ContentId = input.ContentId,
            Locale = input.Locale,
            SourceLocale = input.SourceLocale,
            Content = input.Content,
            ParsedContent = htmlFile
        });
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

}
