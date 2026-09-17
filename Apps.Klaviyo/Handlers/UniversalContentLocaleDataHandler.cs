using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public class UniversalContentLocaleDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] DownloadUniversalContentRequest input)
    : UniversalContentLocaleDataHandlerBase(invocationContext), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context, CancellationToken cancellationToken) =>
        GetLocalesAsync(input.UniversalContentId, context, cancellationToken);
}

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

public abstract class UniversalContentLocaleDataHandlerBase(InvocationContext invocationContext)
    : Invocable(invocationContext)
{
    protected async Task<IEnumerable<DataSourceItem>> GetLocalesAsync(
        string universalContentIdInput, DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(universalContentIdInput))
            throw new PluginMisconfigurationException("Please select universal content first.");

        var universalContentId = UniversalContentFileService.NormalizeUniversalContentId(universalContentIdInput);
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{universalContentId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        cancellationToken.ThrowIfCancellationRequested();

        var translation = response.Data.FirstOrDefault(item =>
            item.Id.StartsWith("template-universal-content::email::", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Relationships["template-universal-content"]?["data"]?["id"]?.ToString(),
                universalContentId, StringComparison.Ordinal));
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
