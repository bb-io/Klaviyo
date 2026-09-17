using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Handlers;

public class DownloadContentDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] DownloadContentRequest input)
    : ContentDataHandlerBase(invocationContext, input.ContentType), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken) => GetContentAsync(context, cancellationToken);
}

public class UploadContentDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] UploadContentRequest input)
    : ContentDataHandlerBase(invocationContext, input.ContentType), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken) => GetContentAsync(context, cancellationToken);
}

public abstract class ContentDataHandlerBase(
    InvocationContext invocationContext,
    string contentType)
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
