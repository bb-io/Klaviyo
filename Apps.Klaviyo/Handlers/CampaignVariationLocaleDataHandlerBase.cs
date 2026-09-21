using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public abstract class CampaignVariationLocaleDataHandlerBase(InvocationContext invocationContext)
    : Invocable(invocationContext)
{
    protected async Task<IEnumerable<DataSourceItem>> GetLocalesAsync(
        string variationIdInput, string? channel, DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(variationIdInput))
            throw new PluginMisconfigurationException("Please select a campaign variation first.");

        var parsed = TranslationChannelHelper.ParseResourceOrTranslationId(
            variationIdInput, TranslationResourceTypes.CampaignVariation,
            TranslationChannels.CampaignVariation,
            TranslationResourceDisplayNames.CampaignVariation);
        var variationId = parsed.ResourceId;
        channel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.CampaignVariation,
            TranslationChannels.CampaignVariation, channel, parsed.Channel);
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{variationId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        cancellationToken.ThrowIfCancellationRequested();

        var translation = response.Data.FirstOrDefault(item =>
            item.Id.StartsWith("campaign-variation::", StringComparison.OrdinalIgnoreCase) &&
            TranslationChannels.CampaignVariation.Contains(
                item.Attributes.Channel, StringComparer.OrdinalIgnoreCase) &&
            (channel is null || string.Equals(item.Attributes.Channel, channel,
                StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(item.Relationships["campaign-variation"]?["data"]?["id"]?.ToString(),
                variationId, StringComparison.Ordinal));
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
