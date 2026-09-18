using Apps.Klaviyo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Handlers;

public class UploadUniversalContentLocaleDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] UploadUniversalContentRequest input)
    : UniversalContentLocaleDataHandlerBase(invocationContext), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(input.UniversalContentId)
            ? Task.FromResult<IEnumerable<DataSourceItem>>([])
            : GetLocalesAsync(input.UniversalContentId, context, cancellationToken);
}
