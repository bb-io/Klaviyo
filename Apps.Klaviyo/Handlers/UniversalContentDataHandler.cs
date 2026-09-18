using Apps.Klaviyo.Api.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public class UniversalContentDataHandler(InvocationContext invocationContext)
    : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new RestRequest("template-universal-content", Method.Get)
            .AddQueryParameter("page[size]", "100");
        var search = context.SearchString?.Trim() ?? string.Empty;
        var results = new List<DataSourceItem>();

        await foreach (var response in Client.PaginateAsync<RelatedResourceDto>(request, cancellationToken))
        {
            results.AddRange(response.Data
                .Where(item => string.Equals(item.Type, "template-universal-content", StringComparison.OrdinalIgnoreCase))
                .Select(item => new
                {
                    item.Id,
                    Name = item.Attributes["name"]?.ToString()
                })
                .Where(item => string.IsNullOrEmpty(search) ||
                               item.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               (item.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(item => new DataSourceItem(item.Id,
                    string.IsNullOrWhiteSpace(item.Name) ? item.Id : $"{item.Name} ({item.Id})")));
        }

        return results.OrderBy(item => item.DisplayName).ToArray();
    }

}
