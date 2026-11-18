using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Identifiers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Klaviyo.Actions;

[ActionList("Templates")]
public class TemplateActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private static readonly string[] SupportedChannels = TranslationChannels.Template;

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
        var (templateId, idChannel) = ParseTemplateId(input.TemplateId);
        var locale = input.Locale?.Trim();
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.Template, SupportedChannels, input.Channel, idChannel);
        var translation = await TranslationResourceHelper.FindTranslationAsync(
            Client, TranslationResourceTypes.Template, templateId, SupportedChannels, requestedChannel);
        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.Template, SupportedChannels, TranslationChannels.Email,
            requestedChannel, translation?.Attributes.Channel,
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
        var suffix = "source";
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
            throw new PluginApplicationException($"Template '{templateId}' has no exportable translation values.");

        if (!string.IsNullOrWhiteSpace(locale) && translation is null)
            throw new PluginMisconfigurationException(
                $"Template '{templateId}' does not have locale '{locale}'.");
        locale = translation is null
            ? null
            : TranslationValuesHelper.ResolveDownloadLocale(
                locale, translation.Attributes.TargetLocales,
                TranslationResourceDisplayNames.Template, templateId);
        suffix = locale ?? "source";

        var exportedValues = TranslationValuesHelper.SelectExportValues(values, locale);
        var metadata = new Dictionary<string, string>
        {
            [TranslationMetadataKeys.ContentType] = TranslationResourceTypes.Template,
            [TranslationMetadataKeys.ResourceId] = templateId,
            [TranslationMetadataKeys.Channel] = channel
        };
        if (translation is not null)
            metadata[TranslationMetadataKeys.TranslationId] = translation.Id;
        var html = TranslationHtmlFileCodec.Export(metadata, exportedValues);
        html = TemplateHtmlFilterService.Create(html, $"{templateId}.{suffix}.html");
        var htmlFile = await TranslationFileService.UploadTextAsync(
            fileManagementClient, html, "text/html", $"{templateId}.{suffix}.html");
        var json = new JObject
        {
            ["data"] = JObject.FromObject(template)
        };
        if (translation is not null)
            json["translation"] = JObject.FromObject(translation);
        var jsonFile = await TranslationFileService.UploadTextAsync(
            fileManagementClient, json.ToString(), "application/json", $"{templateId}.{suffix}.json");

        return new DownloadTemplateResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    [Action("Upload template", Description = "Uploads translated file to the specified existing or new template locale.")]
    public Task UploadTemplate([ActionParameter] UploadTemplateRequest input) =>
        UploadTemplate(input, null);

    internal async Task UploadTemplate(
        UploadTemplateRequest input,
        TranslationHtmlFile? htmlFile)
    {
        var locale = TranslationValuesHelper.ValidateTargetLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        htmlFile ??= await TranslationFileService.ReadAsync(fileManagementClient, input.Content);
        var (templateId, fileChannel) = TranslationChannelHelper.ResolveUploadResourceReference(
            input.TemplateId,
            htmlFile.Metadata,
            TranslationResourceTypes.Template,
            SupportedChannels,
            TranslationResourceDisplayNames.Template);

        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.Template,
            SupportedChannels, TranslationChannels.Email, fileChannel);
        await TranslationUploadService.UploadAsync(
            Client,
            new TranslationUploadContext(
                TranslationResourceTypes.Template,
                TranslationResourceDisplayNames.Template,
                templateId,
                SupportedChannels,
                channel,
                locale,
                input.SourceLocale,
                htmlFile.Values,
                UpdateTargetLocalesBeforeValues: true));
    }

    private async Task<RelatedResourceDto> GetTemplateAsync(string templateId)
    {
        var request = new RestRequest("templates/{templateId}", Method.Get)
            .AddUrlSegment("templateId", templateId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response.Data ?? throw new PluginApplicationException($"Klaviyo returned no template '{templateId}'.");
    }

    private static (string ResourceId, string? Channel) ParseTemplateId(string? templateId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            templateId, TranslationResourceTypes.Template, SupportedChannels,
            TranslationResourceDisplayNames.Template);

}
