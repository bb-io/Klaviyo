using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
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
        var request = new RestRequest("templates", Method.Get);
        var search = context.SearchString?.Trim() ?? string.Empty;
        var results = new List<DataSourceItem>();

        await foreach (var response in Client.PaginateAsync<RelatedResourceDto>(
                           request, pageSize: 10, cancellationToken: cancellationToken))
        {
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
                .Select(item => new DataSourceItem(item.Id, $"{item.Name} (email)")));
        }

        var translationsRequest = new RestRequest("translations", Method.Get)
            .AddQueryParameter("filter", $"equals(resource_type,\"{TranslationResourceTypes.Template}\")")
            .AddQueryParameter("include", TranslationResourceTypes.Template);
        await foreach (var response in Client.PaginateAsync<TranslationDto>(
                           translationsRequest, cancellationToken: cancellationToken))
        {
            var includedById = response.Included
                .Where(item => string.Equals(item.Type, TranslationResourceTypes.Template,
                    StringComparison.OrdinalIgnoreCase))
                .GroupBy(item => item.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            results.AddRange(response.Data
                .Where(item => TranslationChannels.Template.Contains(
                    item.Attributes.Channel, StringComparer.OrdinalIgnoreCase))
                .Select(item => new
                {
                    Id = item.Relationships[TranslationResourceTypes.Template]?["data"]?["id"]?.ToString()
                         ?? item.Id.Split("::", StringSplitOptions.None).LastOrDefault(),
                    item.Attributes.Channel
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(item => new
                {
                    item.Id,
                    item.Channel,
                    Name = includedById.TryGetValue(item.Id!, out var template)
                        ? template.Attributes["name"]?.ToString()
                        : null
                })
                .Where(item => string.IsNullOrEmpty(search) ||
                               item.Id!.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               (item.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(item => new DataSourceItem(item.Id!,
                    string.IsNullOrWhiteSpace(item.Name)
                        ? $"{item.Id} ({item.Channel})"
                        : $"{item.Name} ({item.Channel}, {item.Id})")));
        }

        return results
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(item => item.DisplayName)
            .ToArray();
    }

}
