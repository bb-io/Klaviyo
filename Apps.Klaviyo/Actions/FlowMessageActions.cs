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

[ActionList("Flow messages")]
public class FlowMessageActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private static readonly string[] SupportedChannels = TranslationChannels.FlowMessage;
    private readonly IContentService _contentService =
        new ContentServiceFactory(invocationContext, fileManagementClient)
            .GetContentService(TranslationResourceTypes.FlowMessage);

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
        var result = await _contentService.DownloadAsync(new ContentDownloadServiceRequest
        {
            ContentId = input.FlowMessageId,
            Channel = input.Channel,
            Locale = input.Locale
        });

        return new DownloadFlowMessageResponse
        {
            Content = result.Content,
            JsonFile = result.JsonFile
        };
    }

    [Action("Upload flow message", Description = "Uploads translated file to the specified existing or new flow message locale.")]
    public Task UploadFlowMessage([ActionParameter] UploadFlowMessageRequest input) =>
        _contentService.UploadAsync(new ContentUploadServiceRequest
        {
            ContentId = input.FlowMessageId,
            Locale = input.Locale,
            SourceLocale = input.SourceLocale,
            Content = input.Content
        });

    private static (string ResourceId, string? Channel) ParseFlowMessageId(string? flowMessageId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            flowMessageId, TranslationResourceTypes.FlowMessage, SupportedChannels,
            TranslationResourceDisplayNames.FlowMessage);

}
