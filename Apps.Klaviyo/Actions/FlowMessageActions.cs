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

[ActionList("Flow messages")]
public class FlowMessageActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private static readonly string[] SupportedChannels = TranslationChannels.FlowMessage;

    [Action("Search flow messages", Description = "Searches translations associated with flow messages.")]
    public Task<SearchTranslationsResponse> SearchFlowMessages(
        [ActionParameter] SearchTranslationsRequest input)
    {
        return TranslationSearchHelper.SearchAsync(
            Client, input, [TranslationResourceTypes.FlowMessage], SupportedChannels);
    }

    [Action("Get flow message", Description = "Gets flow message associated with a translation.")]
    public async Task<FlowMessageResponse> GetFlowMessage([ActionParameter] FlowMessageIdentifier input)
    {
        var (flowMessageId, idChannel) = ParseFlowMessageId(input.FlowMessageId);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.FlowMessage, SupportedChannels, input.Channel, idChannel);
        var translation = await TranslationResourceHelper.FindTranslationAsync(
                              Client, TranslationResourceTypes.FlowMessage, flowMessageId,
                              SupportedChannels, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"Flow message '{flowMessageId}' does not have translations configured" +
                              (requestedChannel is null ? "." : $" for channel '{requestedChannel}'."));
        return await TranslationResourceHelper.GetFlowMessageByTranslationIdAsync(Client, translation.Id);
    }

    [Action("Download flow message", Description = "Downloads source or localized flow message values as HTML, with flow message data as JSON.")]
    public async Task<DownloadFlowMessageResponse> DownloadFlowMessage(
        [ActionParameter] DownloadFlowMessageRequest input)
    {
        var (flowMessageId, idChannel) = ParseFlowMessageId(input.FlowMessageId);
        var flowMessage = await GetFlowMessageAsync(flowMessageId);
        var resourceChannel = GetFlowMessageChannel(flowMessage);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.FlowMessage,
            SupportedChannels, input.Channel, idChannel, resourceChannel);
        var translation = await TranslationResourceHelper.FindTranslationAsync(
                              Client, TranslationResourceTypes.FlowMessage, flowMessageId,
                              SupportedChannels, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"Flow message '{flowMessageId}' does not have translations configured yet.");
        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.FlowMessage, SupportedChannels, null,
            requestedChannel, translation.Attributes.Channel,
            TranslationChannelHelper.GetChannelFromTranslationId(translation.Id));
        translation = await TranslationResourceHelper.GetWithValuesAsync(Client, translation.Id);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"Flow message '{flowMessageId}' has no exportable translation values.");

        var locale = TranslationValuesHelper.ResolveDownloadLocale(
            input.Locale, translation.Attributes.TargetLocales,
            TranslationResourceDisplayNames.FlowMessage, flowMessageId);
        var suffix = locale ?? "source";
        var exportedValues = TranslationValuesHelper.SelectExportValues(
            translation.Attributes.Values, locale);
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string>
            {
                [TranslationMetadataKeys.ContentType] = TranslationResourceTypes.FlowMessage,
                [TranslationMetadataKeys.ResourceId] = flowMessageId,
                [TranslationMetadataKeys.Channel] = channel,
                [TranslationMetadataKeys.TranslationId] = translation.Id
            }, exportedValues);
        var fileName = $"{flowMessageId}.{suffix}.html";
        html = TemplateHtmlFilterService.Create(html, fileName);

        var htmlFile = await TranslationFileService.UploadTextAsync(
            fileManagementClient, html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(flowMessage),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await TranslationFileService.UploadTextAsync(fileManagementClient,
            json.ToString(), "application/json",
            $"{flowMessageId}.{suffix}.json");

        return new DownloadFlowMessageResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    [Action("Upload flow message", Description = "Uploads translated file to the specified existing or new flow message locale.")]
    public Task UploadFlowMessage([ActionParameter] UploadFlowMessageRequest input) =>
        UploadFlowMessage(input, null);

    internal async Task UploadFlowMessage(
        UploadFlowMessageRequest input,
        TranslationHtmlFile? htmlFile)
    {
        var locale = TranslationValuesHelper.ValidateTargetLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        htmlFile ??= await TranslationFileService.ReadAsync(fileManagementClient, input.Content);
        var (flowMessageId, fileChannel) = TranslationChannelHelper.ResolveUploadResourceReference(
            input.FlowMessageId,
            htmlFile.Metadata,
            TranslationResourceTypes.FlowMessage,
            SupportedChannels,
            TranslationResourceDisplayNames.FlowMessage);

        var flowMessage = await GetFlowMessageAsync(flowMessageId);
        var resourceChannel = GetFlowMessageChannel(flowMessage);
        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.FlowMessage,
            SupportedChannels, null, fileChannel, resourceChannel);
        await TranslationUploadService.UploadAsync(
            Client,
            new TranslationUploadContext(
                TranslationResourceTypes.FlowMessage,
                TranslationResourceDisplayNames.FlowMessage,
                flowMessageId,
                SupportedChannels,
                channel,
                locale,
                input.SourceLocale,
                htmlFile.Values));
    }

    private static (string ResourceId, string? Channel) ParseFlowMessageId(string? flowMessageId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            flowMessageId, TranslationResourceTypes.FlowMessage, SupportedChannels,
            TranslationResourceDisplayNames.FlowMessage);

    private async Task<RelatedResourceDto> GetFlowMessageAsync(string flowMessageId)
    {
        var request = new RestRequest("flow-messages/{flowMessageId}", Method.Get)
            .AddUrlSegment("flowMessageId", flowMessageId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"Returned no flow message '{flowMessageId}'.");
    }

    private static string GetFlowMessageChannel(RelatedResourceDto flowMessage)
    {
        var channel = flowMessage.Attributes["channel"]?.ToString();
        return TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.FlowMessage, SupportedChannels, null, channel);
    }

}
