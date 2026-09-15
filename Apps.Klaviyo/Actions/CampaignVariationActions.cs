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
        [ActionParameter] TranslationIdentifier input) =>
        new TranslationService(Client).GetCampaignVariationAsync(input.TranslationId);

    [Action("Download campaign variation", Description = "Downloads a campaign variation translation as XLIFF 2.0.")]
    public Task<DownloadTranslationResponse> DownloadCampaignVariation([ActionParameter] DownloadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).DownloadAsync(input, TranslationResourceTypes.CampaignVariation);

    [Action("Upload campaign variation", Description = "Uploads translated XLIFF values to a campaign variation.")]
    public Task UploadCampaignVariation([ActionParameter] UploadTranslationRequest input) =>
        new TranslationFileService(Client, fileManagementClient).UploadAsync(input, TranslationResourceTypes.CampaignVariation);
}
