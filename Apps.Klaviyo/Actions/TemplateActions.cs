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
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Text;

namespace Apps.Klaviyo.Actions;

[ActionList("Templates")]
public class TemplateActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private static readonly string[] SupportedChannels = TranslationChannels.Template;

    [Action("Search templates", Description = "Searches translations associated with templates.")]
    public async Task<SearchTranslationsResponse> SearchTemplates(
        [ActionParameter] SearchTranslationsRequest input)
    {
        DateRangeValidator.Validate(input.UpdatedFrom, input.UpdatedTo);
        var selectedChannels = TranslationSearchHelper.NormalizeAndValidate(
            input.Channels, SupportedChannels, "channel");
        string[] resourceTypes = [TranslationResourceTypes.Template];
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(resource_type,\"{TranslationResourceTypes.Template}\")")
            .AddQueryParameter("include", TranslationResourceTypes.Template);

        var items = new List<TranslationResponse>();
        await foreach (var response in Client.PaginateAsync<TranslationDto>(request))
        {
            var included = response.Included
                .GroupBy(item => $"{item.Type}::{item.Id}")
                .ToDictionary(group => group.Key, group => group.First());
            items.AddRange(response.Data
                .Select(item => TranslationSearchHelper.MapTranslation(item, included))
                .Where(item => TranslationSearchHelper.Matches(
                    item, resourceTypes, selectedChannels, input)));
        }

        return new SearchTranslationsResponse
        {
            Items = items.OrderByDescending(item => item.Updated ?? item.Created ?? DateTime.MinValue).ToList()
        };
    }

    [Action("Get template", Description = "Gets a template associated with a translation.")]
    public async Task<TemplateResponse> GetTemplate([ActionParameter] TemplateIdentifier input)
    {
        var (templateId, idChannel) = ParseTemplateId(input.TemplateId);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            "Template", SupportedChannels, input.Channel, idChannel);
        var translation = await FindTranslationAsync(templateId, requestedChannel)
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
            "Template", SupportedChannels, input.Channel, idChannel);
        var translation = await FindTranslationAsync(templateId, requestedChannel);
        var channel = TranslationChannelHelper.ResolveChannel(
            "Template", SupportedChannels, TranslationChannels.Email,
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
            translation = await GetWithValuesAsync(translation.Id);
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
                locale, translation.Attributes.TargetLocales, "Template", templateId);
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
        var htmlFile = await SaveAsync(html, "text/html", $"{templateId}.{suffix}.html");
        var json = new JObject
        {
            ["data"] = JObject.FromObject(template)
        };
        if (translation is not null)
            json["translation"] = JObject.FromObject(translation);
        var jsonFile = await SaveAsync(json.ToString(), "application/json", $"{templateId}.{suffix}.json");

        return new DownloadTemplateResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    [Action("Upload template", Description = "Uploads translated file to the specified existing or new template locale.")]
    public async Task UploadTemplate([ActionParameter] UploadTemplateRequest input)
    {
        var locale = TranslationValuesHelper.ValidateTargetLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        var htmlFile = await TranslationContentReader.ReadAsync(stream, input.Content.Name);
        var (templateId, fileChannel) = TranslationChannelHelper.ResolveUploadResourceReference(
            input.TemplateId,
            htmlFile.Metadata,
            TranslationResourceTypes.Template,
            SupportedChannels,
            "Template");

        var channel = TranslationChannelHelper.ResolveChannel(
            "Template", SupportedChannels, TranslationChannels.Email, fileChannel);
        var existing = await FindTranslationAsync(templateId, channel);
        locale = existing?.Attributes.TargetLocales.FirstOrDefault(value =>
                     string.Equals(value, locale, StringComparison.OrdinalIgnoreCase)) ?? locale;
        var translation = await EnsureTranslationAsync(
            templateId, locale, input.SourceLocale, channel, existing);
        var current = await GetWithValuesAsync(translation.Id);
        TranslationValuesHelper.ValidateValueIds(
            htmlFile.Values, current.Attributes.Values, "Template", templateId);

        var request = TranslationRequestBuilder.UpdateValues(
            translation.Id, htmlFile.Values, locale);
        await Client.ExecuteWithErrorHandling(request);
    }

    private async Task<RelatedResourceDto> GetTemplateAsync(string templateId)
    {
        var request = new RestRequest("templates/{templateId}", Method.Get)
            .AddUrlSegment("templateId", templateId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response.Data ?? throw new PluginApplicationException($"Klaviyo returned no template '{templateId}'.");
    }

    private async Task<TranslationDto?> FindTranslationAsync(string templateId, string? channel = null)
    {
        var request = TranslationRequestBuilder.FindByResourceId(templateId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        return response.Data.FirstOrDefault(item =>
            item.Id.StartsWith($"{TranslationResourceTypes.Template}::", StringComparison.OrdinalIgnoreCase) &&
            SupportedChannels.Contains(GetChannelFromTranslation(item), StringComparer.OrdinalIgnoreCase) &&
            (channel is null || string.Equals(GetChannelFromTranslation(item), channel,
                StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(item.Relationships["template"]?["data"]?["id"]?.ToString(),
                templateId, StringComparison.Ordinal));
    }

    private async Task<TranslationDto> GetWithValuesAsync(string translationId)
    {
        var request = TranslationRequestBuilder.GetWithValues(translationId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException($"Klaviyo returned no translation '{translationId}'.");
    }

    private async Task<TranslationDto> EnsureTranslationAsync(
        string templateId, string locale, string? sourceLocale, string channel, TranslationDto? existing)
    {
        if (existing is null)
        {
            sourceLocale = TranslationValuesHelper.ValidateSourceLocale(
                sourceLocale, locale, "Template");
            var request = TranslationRequestBuilder.Create(
                TranslationResourceTypes.Template, templateId, sourceLocale, locale, channel);
            var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
            return response.Data ?? throw new PluginApplicationException(
                $"Klaviyo did not return the created translation for '{templateId}'.");
        }

        if (string.Equals(existing.Attributes.SourceLocale, locale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Target locale must differ from source locale.");

        if (existing.Attributes.TargetLocales.Contains(locale, StringComparer.OrdinalIgnoreCase))
            return existing;

        var updateRequest = TranslationRequestBuilder.UpdateTargetLocales(
            existing.Id, TranslationValuesHelper.AddTargetLocale(existing.Attributes.TargetLocales, locale));
        await Client.ExecuteWithErrorHandling(updateRequest);
        return existing;
    }

    private async Task<FileReference> SaveAsync(string content, string mediaType, string fileName) =>
        await fileManagementClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(content)), mediaType, fileName);

    private static (string ResourceId, string? Channel) ParseTemplateId(string? templateId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            templateId, TranslationResourceTypes.Template, SupportedChannels, "Template");

    private static string? GetChannelFromTranslation(TranslationDto translation) =>
        translation.Attributes.Channel ?? TranslationChannelHelper.GetChannelFromTranslationId(translation.Id);
}
