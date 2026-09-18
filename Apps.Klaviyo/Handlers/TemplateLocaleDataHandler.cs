using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public class TemplateLocaleDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] DownloadTemplateRequest input)
    : TemplateLocaleDataHandlerBase(invocationContext), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken) =>
        GetLocalesAsync(input.TemplateId, context, cancellationToken);
}
