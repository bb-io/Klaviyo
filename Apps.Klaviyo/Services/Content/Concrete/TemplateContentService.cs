using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services.Content.Models;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Klaviyo.Services.Content.Concrete;

public class TemplateContentService(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient)
    : BaseContentService(invocationContext, fileManagementClient)
{
    private static readonly string[] SupportedChannels = TranslationChannels.Template;

    public override async Task<ContentDownloadResult> DownloadAsync(ContentDownloadServiceRequest request)
    {
        var (templateId, idChannel) = ParseTemplateId(request.ContentId);
        var locale = request.Locale?.Trim();
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.Template, SupportedChannels, request.Channel, idChannel);
        var translation = await TranslationResourceHelper.FindTranslationAsync(
            Client, TranslationResourceTypes.Template, templateId, SupportedChannels, requestedChannel);
        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.Template,
            SupportedChannels,
            TranslationChannels.Email,
            requestedChannel,
            translation?.Attributes.Channel,
            translation is null
                ? null
                : TranslationChannelHelper.GetChannelFromTranslationId(translation.Id));
        if (translation is null && string.Equals(
                channel, TranslationChannels.WhatsApp, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException(
                $"WhatsApp template '{templateId}' does not have translations configured yet.");

        RelatedResourceDto? untranslatedTemplate = null;
        object template;
        if (translation is null)
        {
            untranslatedTemplate = await GetTemplateAsync(templateId);
            template = untranslatedTemplate;
        }
        else
        {
            template = await TranslationResourceHelper.GetRelatedResourceAsync(
                Client, translation.Id, TranslationResourceTypes.Template);
        }

        IReadOnlyCollection<TranslationValueDto> values;
        if (translation is not null)
        {
            translation = await TranslationResourceHelper.GetWithValuesAsync(Client, translation.Id);
            values = translation.Attributes.Values;
        }
        else
        {
            var sourceHtml = untranslatedTemplate?.Attributes["html"]?.ToString();
            if (string.IsNullOrWhiteSpace(sourceHtml))
                throw new PluginApplicationException($"Template '{templateId}' has no exportable HTML.");
            values =
            [
                new TranslationValueDto
                {
                    Id = $"template::{templateId}::body",
                    SourceValue = sourceHtml
                }
            ];
        }

        if (values.Count == 0)
            throw new PluginApplicationException(
                $"Template '{templateId}' has no exportable translation values.");

        if (!string.IsNullOrWhiteSpace(locale) && translation is null)
            throw new PluginMisconfigurationException(
                $"Template '{templateId}' does not have locale '{locale}'.");
        locale = translation is null
            ? null
            : TranslationValuesHelper.ResolveDownloadLocale(
                locale,
                translation.Attributes.TargetLocales,
                TranslationResourceDisplayNames.Template,
                templateId);
        var suffix = locale ?? "source";

        var exportedValues = TranslationValuesHelper.SelectExportValues(values, locale);
        var metadata = new Dictionary<string, string>
        {
            [TranslationMetadataKeys.ContentType] = TranslationResourceTypes.Template,
            [TranslationMetadataKeys.ResourceId] = templateId,
            [TranslationMetadataKeys.Channel] = channel
        };
        if (translation is not null)
            metadata[TranslationMetadataKeys.TranslationId] = translation.Id;

        var fileName = $"{templateId}.{suffix}.html";
        var html = TranslationHtmlFileCodec.Export(metadata, exportedValues);
        html = TemplateHtmlFilterService.Create(html, fileName);
        var htmlFile = await TranslationFileService.UploadTextAsync(
            FileManagementClient, html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(template)
        };
        if (translation is not null)
            json["translation"] = JObject.FromObject(translation);
        var jsonFile = await TranslationFileService.UploadTextAsync(
            FileManagementClient,
            json.ToString(),
            "application/json",
            $"{templateId}.{suffix}.json");

        return new ContentDownloadResult
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    public override async Task UploadAsync(ContentUploadServiceRequest request)
    {
        var (locale, content) = await PrepareUploadAsync(request);
        var (templateId, fileChannel) = TranslationChannelHelper.ResolveUploadResourceReference(
            request.ContentId,
            content.Metadata,
            TranslationResourceTypes.Template,
            SupportedChannels,
            TranslationResourceDisplayNames.Template);
        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.Template,
            SupportedChannels,
            TranslationChannels.Email,
            fileChannel);

        await TranslationUploadService.UploadAsync(
            Client,
            new TranslationUploadContext(
                TranslationResourceTypes.Template,
                TranslationResourceDisplayNames.Template,
                templateId,
                SupportedChannels,
                channel,
                locale,
                request.SourceLocale,
                content.Values,
                UpdateTargetLocalesBeforeValues: true));
    }

    private async Task<RelatedResourceDto> GetTemplateAsync(string templateId)
    {
        var apiRequest = new RestRequest("templates/{templateId}", Method.Get)
            .AddUrlSegment("templateId", templateId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(apiRequest);
        return response.Data
               ?? throw new PluginApplicationException($"Klaviyo returned no template '{templateId}'.");
    }

    private static (string ResourceId, string? Channel) ParseTemplateId(string? templateId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            templateId,
            TranslationResourceTypes.Template,
            SupportedChannels,
            TranslationResourceDisplayNames.Template);
}
