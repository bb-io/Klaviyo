using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public abstract class TemplateLocaleDataHandlerBase(InvocationContext invocationContext)
    : Invocable(invocationContext)
{
    protected async Task<IEnumerable<DataSourceItem>> GetLocalesAsync(
        string templateIdInput,
        string? channel,
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(templateIdInput))
            throw new PluginMisconfigurationException("Please select a template first.");

        var parsed = TranslationChannelHelper.ParseResourceOrTranslationId(
            templateIdInput, TranslationResourceTypes.Template,
            TranslationChannels.Template, "Template");
        var templateId = parsed.ResourceId;
        channel = TranslationChannelHelper.ResolveOptionalChannel(
            "Template", TranslationChannels.Template, channel, parsed.Channel);
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(related_resource_id,\"{templateId}\")")
            .AddQueryParameter("page[size]", "10");
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        cancellationToken.ThrowIfCancellationRequested();

        var translation = response.Data.FirstOrDefault(item =>
            item.Id.StartsWith("template::", StringComparison.OrdinalIgnoreCase) &&
            TranslationChannels.Template.Contains(
                item.Attributes.Channel, StringComparer.OrdinalIgnoreCase) &&
            (channel is null || string.Equals(item.Attributes.Channel, channel,
                StringComparison.OrdinalIgnoreCase)) &&
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
}
