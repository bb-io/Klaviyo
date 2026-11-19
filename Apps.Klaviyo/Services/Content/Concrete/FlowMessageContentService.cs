using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Services.Content.Models;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.Klaviyo.Services.Content.Concrete;

public class FlowMessageContentService(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient)
    : BaseContentService(invocationContext, fileManagementClient)
{
    private static readonly string[] SupportedChannels = TranslationChannels.FlowMessage;

    public override async Task<ContentDownloadResult> DownloadAsync(ContentDownloadServiceRequest request)
    {
        var (flowMessageId, idChannel) = ParseFlowMessageId(request.ContentId);
        var flowMessage = await GetFlowMessageAsync(flowMessageId);
        var resourceChannel = GetFlowMessageChannel(flowMessage);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.FlowMessage,
            SupportedChannels,
            request.Channel,
            idChannel,
            resourceChannel);

        return await DownloadTranslatedResourceAsync(
            new ContentDownloadServiceRequest
            {
                ContentId = flowMessageId,
                Channel = request.Channel,
                Locale = request.Locale
            },
            TranslationResourceTypes.FlowMessage,
            TranslationResourceDisplayNames.FlowMessage,
            SupportedChannels,
            requestedChannel,
            flowMessage);
    }

    public override async Task UploadAsync(ContentUploadServiceRequest request)
    {
        var (locale, content) = await PrepareUploadAsync(request);
        var (flowMessageId, fileChannel) = TranslationChannelHelper.ResolveUploadResourceReference(
            request.ContentId,
            content.Metadata,
            TranslationResourceTypes.FlowMessage,
            SupportedChannels,
            TranslationResourceDisplayNames.FlowMessage);
        var flowMessage = await GetFlowMessageAsync(flowMessageId);
        var resourceChannel = GetFlowMessageChannel(flowMessage);
        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.FlowMessage,
            SupportedChannels,
            null,
            fileChannel,
            resourceChannel);

        await TranslationUploadService.UploadAsync(
            Client,
            new TranslationUploadContext(
                TranslationResourceTypes.FlowMessage,
                TranslationResourceDisplayNames.FlowMessage,
                flowMessageId,
                SupportedChannels,
                channel,
                locale,
                request.SourceLocale,
                content.Values));
    }

    private async Task<RelatedResourceDto> GetFlowMessageAsync(string flowMessageId)
    {
        var request = new RestRequest("flow-messages/{flowMessageId}", Method.Get)
            .AddUrlSegment("flowMessageId", flowMessageId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<RelatedResourceDto>>(request);
        return response.Data
               ?? throw new PluginApplicationException($"Returned no flow message '{flowMessageId}'.");
    }

    private static string GetFlowMessageChannel(RelatedResourceDto flowMessage)
    {
        var channel = flowMessage.Attributes["channel"]?.ToString();
        return TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.FlowMessage,
            SupportedChannels,
            null,
            channel);
    }

    private static (string ResourceId, string? Channel) ParseFlowMessageId(string? flowMessageId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            flowMessageId,
            TranslationResourceTypes.FlowMessage,
            SupportedChannels,
            TranslationResourceDisplayNames.FlowMessage);
}
