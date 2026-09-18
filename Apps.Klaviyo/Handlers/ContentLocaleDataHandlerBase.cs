using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public abstract class ContentLocaleDataHandlerBase(InvocationContext invocationContext)
    : Invocable(invocationContext)
{
    protected async Task<IEnumerable<DataSourceItem>> GetLocalesAsync(
        string contentType,
        string contentId,
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            throw new PluginMisconfigurationException("Please select 'Content type' first.");
        if (string.IsNullOrWhiteSpace(contentId))
            throw new PluginMisconfigurationException("Please select content first.");

        var (resourceType, resourceId) = Normalize(
            contentType.Trim().ToLowerInvariant(), contentId);
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{resourceId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        cancellationToken.ThrowIfCancellationRequested();

        var translation = response.Data.FirstOrDefault(item =>
            item.Id.StartsWith($"{resourceType}::email::", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Relationships[resourceType]?["data"]?["id"]?.ToString(),
                resourceId, StringComparison.Ordinal));
        var search = context.SearchString?.Trim() ?? string.Empty;

        return translation?.Attributes.TargetLocales
                   .Where(locale => string.IsNullOrWhiteSpace(search) ||
                                    locale.Contains(search, StringComparison.OrdinalIgnoreCase))
                   .OrderBy(locale => locale)
                   .Select(locale => new DataSourceItem(locale, locale))
                   .ToArray()
               ?? [];
    }

    private static (string ResourceType, string ResourceId) Normalize(
        string contentType,
        string contentId) => contentType switch
    {
        TranslationResourceTypes.Template =>
            (contentType, TemplateFileService.NormalizeTemplateId(contentId)),
        TranslationResourceTypes.CampaignVariation =>
            (contentType, CampaignVariationFileService.NormalizeCampaignVariationId(contentId)),
        TranslationResourceTypes.FlowMessage =>
            (contentType, FlowMessageFileService.NormalizeFlowMessageId(contentId)),
        TranslationResourceTypes.UniversalContent =>
            (contentType, UniversalContentFileService.NormalizeUniversalContentId(contentId)),
        _ => throw new PluginMisconfigurationException(
            "Unsupported content type. Select a value from the available options.")
    };
}
