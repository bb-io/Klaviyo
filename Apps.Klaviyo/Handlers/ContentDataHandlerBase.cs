using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Handlers;

public abstract class ContentDataHandlerBase(
    InvocationContext invocationContext,
    string contentType)
    : Invocable(invocationContext)
{
    protected Task<IEnumerable<DataSourceItem>> GetContentAsync(
        DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            throw new PluginMisconfigurationException("Please select 'Content type' first.");

        return contentType.Trim().ToLowerInvariant() switch
        {
            TranslationResourceTypes.Template =>
                new TemplateDataHandler(InvocationContext).GetDataAsync(context, cancellationToken),
            TranslationResourceTypes.CampaignVariation =>
                new CampaignVariationDataHandler(InvocationContext).GetDataAsync(context, cancellationToken),
            TranslationResourceTypes.FlowMessage =>
                new FlowMessageDataHandler(InvocationContext).GetDataAsync(context, cancellationToken),
            TranslationResourceTypes.UniversalContent =>
                new UniversalContentDataHandler(InvocationContext).GetDataAsync(context, cancellationToken),
            _ => throw ExceptionHelper.UnsupportedContentType()
        };
    }
}
