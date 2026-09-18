using Apps.Klaviyo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Handlers;

public class UploadFlowMessageLocaleDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] UploadFlowMessageRequest input)
    : FlowMessageLocaleDataHandlerBase(invocationContext), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(input.FlowMessageId)
            ? Task.FromResult<IEnumerable<DataSourceItem>>([])
            : GetLocalesAsync(input.FlowMessageId, context, cancellationToken);
}
