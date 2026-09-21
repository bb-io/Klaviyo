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

[ActionList("Flow messages")]
public class FlowMessageActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private static readonly string[] SupportedChannels = TranslationChannels.FlowMessage;

    [Action("Search flow messages", Description = "Searches translations associated with flow messages.")]
    public async Task<SearchTranslationsResponse> SearchFlowMessages(
        [ActionParameter] SearchTranslationsRequest input)
    {
        DateRangeValidator.Validate(input.UpdatedFrom, input.UpdatedTo);
        var selectedChannels = TranslationSearchHelper.NormalizeAndValidate(
            input.Channels, SupportedChannels, "channel");
        string[] resourceTypes = [TranslationResourceTypes.FlowMessage];
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("include", TranslationResourceTypes.FlowMessage);

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

    [Action("Get flow message", Description = "Gets flow message associated with a translation.")]
    public async Task<FlowMessageResponse> GetFlowMessage([ActionParameter] FlowMessageIdentifier input)
    {
        var (flowMessageId, idChannel) = ParseFlowMessageId(input.FlowMessageId);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            "Flow message", SupportedChannels, input.Channel, idChannel);
        var translation = await FindTranslationAsync(flowMessageId, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"Flow message '{flowMessageId}' does not have translations configured" +
                              (requestedChannel is null ? "." : $" for channel '{requestedChannel}'."));
        return await ContentActions.GetFlowMessageAsync(Client, translation.Id);
    }

    [Action("Download flow message", Description = "Downloads source or localized flow message values as HTML, with flow message data as JSON.")]
    public async Task<DownloadFlowMessageResponse> DownloadFlowMessage(
        [ActionParameter] DownloadFlowMessageRequest input)
    {
        var (flowMessageId, idChannel) = ParseFlowMessageId(input.FlowMessageId);
        var flowMessage = await GetFlowMessageAsync(flowMessageId);
        var resourceChannel = GetFlowMessageChannel(flowMessage);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            "Flow message", SupportedChannels, input.Channel, idChannel, resourceChannel);
        var translation = await FindTranslationAsync(flowMessageId, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"Flow message '{flowMessageId}' does not have translations configured yet.");
        var channel = TranslationChannelHelper.ResolveChannel(
            "Flow message", SupportedChannels, null,
            requestedChannel, translation.Attributes.Channel,
            TranslationChannelHelper.GetChannelFromTranslationId(translation.Id));
        translation = await GetWithValuesAsync(translation.Id);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"Flow message '{flowMessageId}' has no exportable translation values.");

        var locale = TranslationValuesHelper.ResolveDownloadLocale(
            input.Locale, translation.Attributes.TargetLocales, "Flow message", flowMessageId);
        var suffix = locale ?? "source";
        var exportedValues = TranslationValuesHelper.SelectExportValues(
            translation.Attributes.Values, locale);
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string>
            {
                ["ContentType"] = TranslationResourceTypes.FlowMessage,
                ["FlowMessageId"] = flowMessageId,
                ["Channel"] = channel,
                ["TranslationId"] = translation.Id
            }, exportedValues);
        var fileName = $"{flowMessageId}.{suffix}.html";
        html = TemplateHtmlFilterService.Create(html, fileName);

        var htmlFile = await SaveAsync(html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(flowMessage),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await SaveAsync(json.ToString(), "application/json",
            $"{flowMessageId}.{suffix}.json");

        return new DownloadFlowMessageResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    [Action("Upload flow message", Description = "Uploads translated file to the specified existing or new flow message locale.")]
    public async Task UploadFlowMessage([ActionParameter] UploadFlowMessageRequest input)
    {
        var locale = TranslationValuesHelper.ValidateTargetLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        var htmlFile = await TranslationContentReader.ReadAsync(stream, input.Content.Name);
        var (flowMessageId, fileChannel) = TranslationChannelHelper.ResolveUploadResourceReference(
            input.FlowMessageId,
            htmlFile.Metadata,
            "FlowMessageId",
            TranslationResourceTypes.FlowMessage,
            SupportedChannels,
            "Flow message");

        var flowMessage = await GetFlowMessageAsync(flowMessageId);
        var resourceChannel = GetFlowMessageChannel(flowMessage);
        var channel = TranslationChannelHelper.ResolveChannel(
            "Flow message", SupportedChannels, null, fileChannel, resourceChannel);
        var existing = await FindTranslationAsync(flowMessageId, channel);
        TranslationDto translation;
        if (existing is null)
        {
            translation = await CreateTranslationAsync(flowMessageId, locale, input.SourceLocale, channel);
        }
        else
        {
            if (string.Equals(existing.Attributes.SourceLocale, locale, StringComparison.OrdinalIgnoreCase))
                throw new PluginMisconfigurationException("Target locale must differ from source locale.");
            locale = existing.Attributes.TargetLocales.FirstOrDefault(value =>
                         string.Equals(value, locale, StringComparison.OrdinalIgnoreCase)) ?? locale;
            translation = existing;
        }

        var current = await GetWithValuesAsync(translation.Id);
        TranslationValuesHelper.ValidateValueIds(
            htmlFile.Values, current.Attributes.Values, "Flow message", flowMessageId);

        var targetLocales = current.Attributes.TargetLocales.Contains(locale, StringComparer.OrdinalIgnoreCase)
            ? null
            : TranslationValuesHelper.AddTargetLocale(current.Attributes.TargetLocales, locale);
        var request = TranslationRequestBuilder.UpdateValues(
            current.Id, htmlFile.Values, locale, targetLocales);
        await Client.ExecuteWithErrorHandling(request);
    }

    private static (string ResourceId, string? Channel) ParseFlowMessageId(string? flowMessageId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            flowMessageId, TranslationResourceTypes.FlowMessage, SupportedChannels, "Flow message");

    private async Task<RelatedResourceDto> GetFlowMessageAsync(string flowMessageId)
    {
        var request = new RestRequest("flow-messages/{flowMessageId}", Method.Get)
            .AddUrlSegment("flowMessageId", flowMessageId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Returned no flow message '{flowMessageId}'.");
    }

    private async Task<TranslationDto?> FindTranslationAsync(string flowMessageId, string? channel = null)
    {
        var request = TranslationRequestBuilder.FindByResourceId(flowMessageId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        return response.Data.FirstOrDefault(item =>
            item.Id.StartsWith($"{TranslationResourceTypes.FlowMessage}::", StringComparison.OrdinalIgnoreCase) &&
            SupportedChannels.Contains(GetChannelFromTranslation(item), StringComparer.OrdinalIgnoreCase) &&
            (channel is null || string.Equals(GetChannelFromTranslation(item), channel,
                StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(item.Relationships[TranslationResourceTypes.FlowMessage]?["data"]?["id"]?.ToString(),
                flowMessageId, StringComparison.Ordinal));
    }

    private async Task<TranslationDto> GetWithValuesAsync(string translationId)
    {
        var request = TranslationRequestBuilder.GetWithValues(translationId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Returned no translation '{translationId}'.");
    }

    private async Task<TranslationDto> CreateTranslationAsync(
        string flowMessageId, string locale, string? sourceLocale, string channel)
    {
        sourceLocale = TranslationValuesHelper.ValidateSourceLocale(
            sourceLocale, locale, "Flow message");
        var request = TranslationRequestBuilder.Create(
            TranslationResourceTypes.FlowMessage, flowMessageId, sourceLocale, locale, channel);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Klaviyo did not return the created translation for '{flowMessageId}'.");
    }

    private static string GetFlowMessageChannel(RelatedResourceDto flowMessage)
    {
        var channel = flowMessage.Attributes["channel"]?.ToString();
        return TranslationChannelHelper.ResolveChannel(
            "Flow message", SupportedChannels, null, channel);
    }

    private static string? GetChannelFromTranslation(TranslationDto translation) =>
        translation.Attributes.Channel ?? TranslationChannelHelper.GetChannelFromTranslationId(translation.Id);

    private async Task<FileReference> SaveAsync(string content, string mediaType, string fileName) =>
        await fileManagementClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(content)), mediaType, fileName);

}
