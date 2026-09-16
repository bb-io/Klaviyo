using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public class FlowMessageLocaleDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] DownloadFlowMessageRequest input)
    : FlowMessageLocaleDataHandlerBase(invocationContext), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context, CancellationToken cancellationToken) =>
        GetLocalesAsync(input.FlowMessageId, context, cancellationToken);
}

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

public abstract class FlowMessageLocaleDataHandlerBase(InvocationContext invocationContext)
    : Invocable(invocationContext)
{
    protected async Task<IEnumerable<DataSourceItem>> GetLocalesAsync(
        string flowMessageIdInput, DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowMessageIdInput))
            throw new PluginMisconfigurationException("Please select a flow message first.");

        var flowMessageId = FlowMessageFileService.NormalizeFlowMessageId(flowMessageIdInput);
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{flowMessageId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        cancellationToken.ThrowIfCancellationRequested();

        var translation = response.Data.FirstOrDefault(item =>
            item.Id.StartsWith("flow-message::email::", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Relationships["flow-message"]?["data"]?["id"]?.ToString(),
                flowMessageId, StringComparison.Ordinal));
        var search = context.SearchString?.Trim() ?? string.Empty;

        return translation?.Attributes.TargetLocales
                   .Where(locale => string.IsNullOrWhiteSpace(search) ||
                                    locale.Contains(search, StringComparison.OrdinalIgnoreCase))
                   .OrderBy(locale => locale)
                   .Select(locale => new DataSourceItem(locale, locale))
                   .ToArray()
               ?? [];
    }
}
