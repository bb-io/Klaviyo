using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Actions;

[ActionList("Flow messages")]
public class FlowMessageActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [Action("Search flow messages", Description = "Searches translations associated with flow messages.")]
    public Task<SearchTranslationsResponse> SearchFlowMessages(
        [ActionParameter] SearchTranslationsRequest input) =>
        new TranslationService(Client).SearchAsync(input, [TranslationResourceTypes.FlowMessage]);

    [Action("Download flow message", Description = "Downloads a flow message translation as XLIFF 2.0.")]
    public Task<DownloadTranslationResponse> DownloadFlowMessage([ActionParameter] DownloadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).DownloadAsync(input, TranslationResourceTypes.FlowMessage);

    [Action("Upload flow message", Description = "Uploads translated XLIFF values to a flow message.")]
    public Task UploadFlowMessage([ActionParameter] UploadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).UploadAsync(input, TranslationResourceTypes.FlowMessage);
}
