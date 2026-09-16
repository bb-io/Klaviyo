using Apps.Klaviyo.Api.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public class CampaignVariationDataHandler(InvocationContext invocationContext)
    : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new RestRequest("campaign-messages", Method.Get)
            .AddQueryParameter("include", "campaign-variations")
            .AddQueryParameter("page[size]", "100");
        var search = context.SearchString?.Trim() ?? string.Empty;
        var results = new List<DataSourceItem>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<RelatedResourceDto>>(request);
            results.AddRange(response.Included
                .Where(item => string.Equals(item.Type, "campaign-variation", StringComparison.OrdinalIgnoreCase))
                .Select(item => new
                {
                    item.Id,
                    Name = item.Attributes.SelectToken("definition.name")?.ToString(),
                    Channel = item.Attributes.SelectToken("definition.details.channel")?.ToString()
                })
                .Where(item => string.Equals(item.Channel, "email", StringComparison.OrdinalIgnoreCase))
                .Where(item => string.IsNullOrEmpty(search) ||
                               item.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               (item.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(item => new DataSourceItem(item.Id,
                    string.IsNullOrWhiteSpace(item.Name) ? item.Id : $"{item.Name} ({item.Id})")));

            var next = response.Links?.Next;
            if (string.IsNullOrWhiteSpace(next) || !visited.Add(next))
                break;
            request = new RestRequest(next, Method.Get);
        }

        return results.OrderBy(item => item.DisplayName).ToArray();
    }
}
