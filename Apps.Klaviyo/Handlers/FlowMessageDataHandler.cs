using Apps.Klaviyo.Api.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Klaviyo.Handlers;

public class FlowMessageDataHandler(InvocationContext invocationContext)
    : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("include", "flow-message")
            .AddQueryParameter("page[size]", "100");
        var search = context.SearchString?.Trim() ?? string.Empty;
        var results = new List<DataSourceItem>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
            var includedById = response.Included
                .Where(item => string.Equals(item.Type, "flow-message", StringComparison.OrdinalIgnoreCase))
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            results.AddRange(response.Data
                .Where(item => item.Id.StartsWith("flow-message::email::", StringComparison.OrdinalIgnoreCase))
                .Select(item => new
                {
                    Id = item.Relationships["flow-message"]?["data"]?["id"]?.ToString()
                         ?? item.Id.Split("::", StringSplitOptions.None).LastOrDefault(),
                    Name = GetName(item, includedById)
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .Where(item => string.IsNullOrEmpty(search) ||
                               item.Id!.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               (item.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(item => new DataSourceItem(item.Id!,
                    string.IsNullOrWhiteSpace(item.Name) ? item.Id! : $"{item.Name} ({item.Id})")));

            var next = response.Links?.Next;
            if (string.IsNullOrWhiteSpace(next) || !visited.Add(next))
                break;
            request = new RestRequest(next, Method.Get);
        }

        return results
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(item => item.DisplayName)
            .ToArray();
    }

    private static string? GetName(
        TranslationDto translation,
        IReadOnlyDictionary<string, RelatedResourceDto> includedById)
    {
        var id = translation.Relationships["flow-message"]?["data"]?["id"]?.ToString()
                 ?? translation.Id.Split("::", StringSplitOptions.None).LastOrDefault();
        return id is not null && includedById.TryGetValue(id, out var resource)
            ? resource.Attributes["name"]?.ToString()
            : null;
    }
}
