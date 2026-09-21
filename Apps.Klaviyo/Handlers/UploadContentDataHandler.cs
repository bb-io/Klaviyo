using Apps.Klaviyo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Handlers;

public class UploadContentDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] UploadContentRequest input)
    : ContentDataHandlerBase(invocationContext, input.ContentType), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken) => GetContentAsync(context, cancellationToken);
}
