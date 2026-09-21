using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public abstract class FlowMessageLocaleDataHandlerBase(InvocationContext invocationContext)
    : Invocable(invocationContext)
{
    protected async Task<IEnumerable<DataSourceItem>> GetLocalesAsync(
        string flowMessageIdInput, string? channel, DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowMessageIdInput))
            throw new PluginMisconfigurationException("Please select a flow message first.");

        var parsed = TranslationChannelHelper.ParseResourceOrTranslationId(
            flowMessageIdInput, TranslationResourceTypes.FlowMessage,
            TranslationChannels.FlowMessage, "Flow message");
        var flowMessageId = parsed.ResourceId;
        channel = TranslationChannelHelper.ResolveOptionalChannel(
            "Flow message", TranslationChannels.FlowMessage, channel, parsed.Channel);
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{flowMessageId}\")")
            .AddQueryParameter("page[size]", "100");
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        cancellationToken.ThrowIfCancellationRequested();

        var translation = response.Data.FirstOrDefault(item =>
            item.Id.StartsWith("flow-message::", StringComparison.OrdinalIgnoreCase) &&
            TranslationChannels.FlowMessage.Contains(
                item.Attributes.Channel, StringComparer.OrdinalIgnoreCase) &&
            (channel is null || string.Equals(item.Attributes.Channel, channel,
                StringComparison.OrdinalIgnoreCase)) &&
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
