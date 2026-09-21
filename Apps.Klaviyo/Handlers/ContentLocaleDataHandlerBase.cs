using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public abstract class ContentLocaleDataHandlerBase(InvocationContext invocationContext)
    : Invocable(invocationContext)
{
    protected async Task<IEnumerable<DataSourceItem>> GetLocalesAsync(
        string? contentType,
        string contentId,
        string? channel,
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            throw new PluginMisconfigurationException("Please select 'Content type' first.");
        if (string.IsNullOrWhiteSpace(contentId))
            throw new PluginMisconfigurationException("Please select content first.");

        var resourceType = NormalizeResourceType(contentType.Trim().ToLowerInvariant());
        var supportedChannels = TranslationChannels.ForResourceType(resourceType);
        var parsed = TranslationChannelHelper.ParseResourceOrTranslationId(
            contentId, resourceType, supportedChannels, TranslationResourceDisplayNames.Content);
        var resourceId = parsed.ResourceId;
        channel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.Content, supportedChannels, channel, parsed.Channel);
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{resourceId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        cancellationToken.ThrowIfCancellationRequested();

        var translation = response.Data.FirstOrDefault(item =>
            item.Id.StartsWith($"{resourceType}::", StringComparison.OrdinalIgnoreCase) &&
            supportedChannels.Contains(
                item.Attributes.Channel ?? GetChannelFromId(item.Id), StringComparer.OrdinalIgnoreCase) &&
            (channel is null || string.Equals(
                item.Attributes.Channel ?? GetChannelFromId(item.Id), channel,
                StringComparison.OrdinalIgnoreCase)) &&
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

    private static string NormalizeResourceType(string contentType) => contentType switch
    {
        TranslationResourceTypes.Template => contentType,
        TranslationResourceTypes.CampaignVariation => contentType,
        TranslationResourceTypes.FlowMessage => contentType,
        TranslationResourceTypes.UniversalContent => contentType,
        _ => throw ExceptionHelper.UnsupportedContentType()
    };

    private static string? GetChannelFromId(string translationId)
    {
        var parts = translationId.Split("::", StringSplitOptions.None);
        return parts.Length == 3 ? parts[1] : null;
    }
}
