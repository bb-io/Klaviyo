using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Polling;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using RestSharp;

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
        DateRangeValidator.Validate(lastPollingTime, pollingStartedAt);
        var searchResult = await SearchContentAsync(
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

    private async Task<SearchTranslationsResponse> SearchContentAsync(
        SearchTranslationsRequest input,
        IEnumerable<string>? resourceTypes)
    {
        var selectedChannels = TranslationSearchHelper.NormalizeAndValidate(
            input.Channels, TranslationChannels.All, "channel");
        var requestedResourceTypes = resourceTypes?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        var selectedResourceTypes = TranslationSearchHelper.NormalizeAndValidate(
            requestedResourceTypes is { Length: > 0 } ? requestedResourceTypes : TranslationResourceTypes.All,
            TranslationResourceTypes.All,
            "content type");

        var apiRequest = new RestRequest("translations", Method.Get);
        if (selectedResourceTypes.Count == 1 &&
            !selectedResourceTypes[0].Equals(TranslationResourceTypes.CampaignVariation,
                StringComparison.OrdinalIgnoreCase) &&
            !selectedResourceTypes[0].Equals(TranslationResourceTypes.FlowMessage,
                StringComparison.OrdinalIgnoreCase) &&
            !selectedResourceTypes[0].Equals(TranslationResourceTypes.UniversalContent,
                StringComparison.OrdinalIgnoreCase))
            apiRequest.AddQueryParameter("filter", $"equals(resource_type,\"{selectedResourceTypes[0]}\")");
        apiRequest.AddQueryParameter("include", string.Join(',', selectedResourceTypes));

        var items = new List<TranslationResponse>();
        await foreach (var response in Client.PaginateAsync<TranslationDto>(apiRequest))
        {
            var included = response.Included
                .GroupBy(item => $"{item.Type}::{item.Id}")
                .ToDictionary(group => group.Key, group => group.First());
            items.AddRange(response.Data
                .Select(item => TranslationSearchHelper.MapTranslation(item, included))
                .Where(item => TranslationSearchHelper.Matches(
                    item, selectedResourceTypes, selectedChannels, input)));
        }

        return new SearchTranslationsResponse
        {
            Items = items.OrderByDescending(item => item.Updated ?? item.Created ?? DateTime.MinValue).ToList()
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
