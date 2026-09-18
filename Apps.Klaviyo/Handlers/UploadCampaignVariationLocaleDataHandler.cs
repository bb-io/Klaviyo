using Apps.Klaviyo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Handlers;

public class UploadCampaignVariationLocaleDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] UploadCampaignVariationRequest input)
    : CampaignVariationLocaleDataHandlerBase(invocationContext), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(input.CampaignVariationId)
            ? Task.FromResult<IEnumerable<DataSourceItem>>([])
            : GetLocalesAsync(input.CampaignVariationId, context, cancellationToken);
}
