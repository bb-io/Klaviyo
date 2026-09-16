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

    [Action("Download flow message", Description = "Downloads source or localized flow message values as HTML, with flow message data as JSON.")]
    public Task<DownloadFlowMessageResponse> DownloadFlowMessage([ActionParameter] DownloadFlowMessageRequest input) =>
        new FlowMessageFileService(Client, fileManagementClient).DownloadAsync(input);

    [Action("Upload flow message", Description = "Uploads translated HTML to the specified existing or new flow message locale.")]
    public Task UploadFlowMessage([ActionParameter] UploadFlowMessageRequest input) =>
        new FlowMessageFileService(Client, fileManagementClient).UploadAsync(input);
}
