using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Services.Content.Models;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;

namespace Apps.Klaviyo.Services.Content;

public abstract class BaseContentService(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext), IContentService
{
    protected IFileManagementClient FileManagementClient { get; } = fileManagementClient;

    public abstract Task<ContentDownloadResult> DownloadAsync(ContentDownloadServiceRequest request);

    public abstract Task UploadAsync(ContentUploadServiceRequest request);

    protected async Task<(string Locale, TranslationHtmlFile Content)> PrepareUploadAsync(
        ContentUploadServiceRequest request)
    {
        var locale = TranslationValuesHelper.ValidateTargetLocale(request.Locale);
        if (request.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        var content = request.ParsedContent
                      ?? await TranslationFileService.ReadAsync(FileManagementClient, request.Content);
        return (locale, content);
    }

    protected async Task<ContentDownloadResult> DownloadTranslatedResourceAsync(
        ContentDownloadServiceRequest request,
        string resourceType,
        string resourceDisplayName,
        IReadOnlyCollection<string> supportedChannels,
        string? requestedChannel,
        object resource)
    {
        var translation = await TranslationResourceHelper.FindTranslationAsync(
                              Client, resourceType, request.ContentId, supportedChannels, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"{resourceDisplayName} '{request.ContentId}' does not have translations configured yet.");
        var channel = TranslationChannelHelper.ResolveChannel(
            resourceDisplayName,
            supportedChannels,
            null,
            requestedChannel,
            translation.Attributes.Channel,
            TranslationChannelHelper.GetChannelFromTranslationId(translation.Id));
        translation = await TranslationResourceHelper.GetWithValuesAsync(Client, translation.Id);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"{resourceDisplayName} '{request.ContentId}' has no exportable translation values.");

        var locale = TranslationValuesHelper.ResolveDownloadLocale(
            request.Locale,
            translation.Attributes.TargetLocales,
            resourceDisplayName,
            request.ContentId);
        var suffix = locale ?? "source";
        var exportedValues = TranslationValuesHelper.SelectExportValues(
            translation.Attributes.Values, locale);
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string>
            {
                [TranslationMetadataKeys.ContentType] = resourceType,
                [TranslationMetadataKeys.ResourceId] = request.ContentId,
                [TranslationMetadataKeys.Channel] = channel,
                [TranslationMetadataKeys.TranslationId] = translation.Id
            },
            exportedValues);
        var fileName = $"{request.ContentId}.{suffix}.html";
        html = TemplateHtmlFilterService.Create(html, fileName);

        var htmlFile = await TranslationFileService.UploadTextAsync(
            FileManagementClient, html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(resource),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await TranslationFileService.UploadTextAsync(
            FileManagementClient,
            json.ToString(),
            "application/json",
            $"{request.ContentId}.{suffix}.json");

        return new ContentDownloadResult
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }
}
