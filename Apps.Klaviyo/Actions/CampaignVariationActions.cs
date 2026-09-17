using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Actions;

[ActionList("Campaign variations")]
public class CampaignVariationActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [Action("Search campaign variations",
        Description = "Searches translations associated with campaign variations.")]
    public Task<SearchTranslationsResponse> SearchCampaignVariations(
        [ActionParameter] SearchTranslationsRequest input) =>
        new TranslationService(Client).SearchAsync(input, [TranslationResourceTypes.CampaignVariation]);

    [Action("Get campaign variation",
        Description = "Gets campaign variation associated with a translation.")]
    public Task<CampaignVariationResponse> GetCampaignVariation(
        [ActionParameter] CampaignVariationIdentifier input)
    {
        var variationId = CampaignVariationFileService.NormalizeCampaignVariationId(
            input.CampaignVariationId);
        return new TranslationService(Client).GetCampaignVariationAsync(
            $"campaign-variation::email::{variationId}");
    }

    [Action("Download campaign variation", Description = "Downloads source or localized campaign variation values as HTML, with campaign variation data as JSON.")]
    public Task<DownloadCampaignVariationResponse> DownloadCampaignVariation(
        [ActionParameter] DownloadCampaignVariationRequest input) =>
        new CampaignVariationFileService(Client, fileManagementClient).DownloadAsync(input);

    [Action("Upload campaign variation", Description = "Uploads translated HTML to the specified existing or new campaign variation locale.")]
    public Task UploadCampaignVariation([ActionParameter] UploadCampaignVariationRequest input) =>
        new CampaignVariationFileService(Client, fileManagementClient).UploadAsync(input);
}
