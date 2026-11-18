using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Klaviyo.Handlers;

public class DownloadContentChannelDataHandler(
    [ActionParameter] DownloadContentRequest input) : IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(
        DataSourceContext context,
        CancellationToken cancellationToken) => Task.FromResult(GetChannels(input.ContentType, context));

    internal static IEnumerable<DataSourceItem> GetChannels(string? resourceType, DataSourceContext context)
    {
        var search = context.SearchString?.Trim() ?? string.Empty;
        return TranslationChannels.ForResourceType(resourceType?.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(channel => string.IsNullOrWhiteSpace(search) ||
                              channel.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Select(channel => new DataSourceItem(channel, channel switch
            {
                TranslationChannels.Email => "Email",
                TranslationChannels.Sms => "SMS",
                TranslationChannels.MobilePush => "Mobile push",
                TranslationChannels.WhatsApp => "WhatsApp",
                _ => channel
            }));
    }
}
