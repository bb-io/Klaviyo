using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Handlers;

public class ContentDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] ContentTypeFilter filter) : IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.ContentType))
            throw new PluginMisconfigurationException("Please select 'Content type' first.");

        return filter.ContentType.Trim().ToLowerInvariant() switch
        {
            TranslationResourceTypes.Template =>
                new TemplateDataHandler(invocationContext).GetDataAsync(context, cancellationToken),
            TranslationResourceTypes.CampaignVariation =>
                new CampaignVariationDataHandler(invocationContext).GetDataAsync(context, cancellationToken),
            TranslationResourceTypes.FlowMessage =>
                new FlowMessageDataHandler(invocationContext).GetDataAsync(context, cancellationToken),
            TranslationResourceTypes.UniversalContent =>
                new UniversalContentDataHandler(invocationContext).GetDataAsync(context, cancellationToken),
            _ => throw new PluginMisconfigurationException(
                "Unsupported content type. Select a value from the available options.")
        };
    }
}
