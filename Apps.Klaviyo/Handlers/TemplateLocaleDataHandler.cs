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

public abstract class TemplateLocaleDataHandlerBase(InvocationContext invocationContext)
    : Invocable(invocationContext)
{
    protected async Task<IEnumerable<DataSourceItem>> GetLocalesAsync(
        string templateIdInput,
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(templateIdInput))
            throw new PluginMisconfigurationException("Please select a template first.");

        var templateId = NormalizeTemplateId(templateIdInput);
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{templateId}\")")
            .AddQueryParameter("page[size]", "10");
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        cancellationToken.ThrowIfCancellationRequested();

        var translation = response.Data.FirstOrDefault(item =>
            item.Id.StartsWith("template::email::", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Relationships["template"]?["data"]?["id"]?.ToString(),
                templateId, StringComparison.Ordinal));
        var search = context.SearchString?.Trim() ?? string.Empty;

        return translation?.Attributes.TargetLocales
                   .Where(locale => string.IsNullOrWhiteSpace(search) ||
                                    locale.Contains(search, StringComparison.OrdinalIgnoreCase))
                   .OrderBy(locale => locale)
                   .Select(locale => new DataSourceItem(locale, locale))
                   .ToArray()
               ?? [];
    }

    private static string NormalizeTemplateId(string templateId)
    {
        var normalized = templateId.Trim();
        if (!normalized.Contains("::", StringComparison.Ordinal))
            return normalized;

        var parts = normalized.Split("::", StringSplitOptions.None);
        if (parts.Length == 3 &&
            string.Equals(parts[0], "template", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(parts[1], "email", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(parts[2]))
            return parts[2];

        throw new PluginMisconfigurationException(
            "Expected an email template ID or template::email:: translation ID.");
    }
}
