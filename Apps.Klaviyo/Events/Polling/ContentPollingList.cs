using Apps.Klaviyo.Models.Polling;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.Klaviyo.Events.Polling;

[PollingEventList("Content")]
public class ContentPollingList(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [BlueprintEventDefinition(BlueprintEvent.ContentCreatedOrUpdatedMultiple)]
    [PollingEvent("On content added or updated",
        Description = "Periodically checks for Klaviyo content added or updated since the previous poll.")]
    public async Task<PollingEventResponse<PollingMemory, ContentUpdatedMultipleResponse>> OnContentAddedOrUpdated(
        PollingEventRequest<PollingMemory> request,
        [PollingEventParameter] ContentPollingFilter filter)
    {
        var pollingStartedAt = DateTime.UtcNow;
        if (request.Memory?.LastPollingTime is null)
            return Baseline<ContentUpdatedMultipleResponse>(pollingStartedAt);

        var lastPollingTime = request.Memory.LastPollingTime.Value;
        var searchResult = await new TranslationService(Client).SearchAsync(
            new SearchTranslationsRequest
            {
                UpdatedFrom = lastPollingTime,
                UpdatedTo = pollingStartedAt
            },
            filter.ContentTypes);

        var items = searchResult.Items
            .Where(item => item.Updated > lastPollingTime && item.Updated <= pollingStartedAt)
            .OrderBy(item => item.Updated)
            .Select(item => new ContentUpdatedItem
            {
                ContentId = item.ContentId,
                ContentType = item.ResourceType,
                ResourceId = item.ResourceId,
                Name = item.Name,
                Channel = item.Channel,
                SourceLocale = item.SourceLocale,
                TargetLocales = item.TargetLocales,
                FallbackLocale = item.FallbackLocale,
                Created = item.Created,
                Updated = item.Updated
            })
            .ToList();

        return new PollingEventResponse<PollingMemory, ContentUpdatedMultipleResponse>
        {
            FlyBird = items.Count > 0,
            Memory = new PollingMemory { LastPollingTime = pollingStartedAt },
            Result = items.Count > 0
                ? new ContentUpdatedMultipleResponse { Items = items }
                : null
        };
    }

    private static PollingEventResponse<PollingMemory, TResult> Baseline<TResult>(DateTime pollingStartedAt)
        where TResult : class =>
        new()
        {
            FlyBird = false,
            Memory = new PollingMemory { LastPollingTime = pollingStartedAt },
            Result = null
        };
}
