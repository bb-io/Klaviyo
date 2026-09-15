using Apps.Klaviyo.Api.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public class TemplateDataHandler(InvocationContext invocationContext)
    : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new RestRequest("templates", Method.Get)
            .AddQueryParameter("page[size]", "100");
        var search = context.SearchString?.Trim() ?? string.Empty;
        var results = new List<DataSourceItem>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<RelatedResourceDto>>(request);
            results.AddRange(response.Data
                .Where(item => item.Type == "template")
                .Select(item => new
                {
                    item.Id,
                    Name = item.Attributes["name"]?.ToString() ?? item.Id
                })
                .Where(item => string.IsNullOrEmpty(search) ||
                               item.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               item.Id.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(item => new DataSourceItem(item.Id, item.Name)));

            var next = response.Links?.Next;
            if (string.IsNullOrWhiteSpace(next) || !visited.Add(next))
                break;
            request = new RestRequest(next, Method.Get);
        }

        return results.OrderBy(item => item.DisplayName).ToArray();
    }
}
