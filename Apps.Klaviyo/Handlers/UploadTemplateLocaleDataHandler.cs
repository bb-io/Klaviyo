using Apps.Klaviyo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Handlers;

public class UploadTemplateLocaleDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] UploadTemplateRequest input)
    : TemplateLocaleDataHandlerBase(invocationContext), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(input.TemplateId)
            ? Task.FromResult<IEnumerable<DataSourceItem>>([])
            : GetLocalesAsync(input.TemplateId, context, cancellationToken);
}
