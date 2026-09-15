using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Klaviyo.Actions;

[ActionList("Campaign variations")]
public class CampaignVariationActions(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [Action("Search campaign variations",
        Description = "Searches translations associated with campaign variations.")]
    public Task<SearchTranslationsResponse> SearchCampaignVariations(
        [ActionParameter] SearchTranslationsRequest input) =>
        new TranslationService(Client).SearchAsync(input, [TranslationResourceTypes.CampaignVariation]);

    [Action("Get campaign variation",
        Description = "Gets campaign variation associated with a translation.")]
    public Task<CampaignVariationResponse> GetCampaignVariation(
        [ActionParameter] TranslationIdentifier input) =>
        new TranslationService(Client).GetCampaignVariationAsync(input.TranslationId);
}
