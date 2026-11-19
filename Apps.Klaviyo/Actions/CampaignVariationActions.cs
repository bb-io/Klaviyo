using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Identifiers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services.Content;
using Apps.Klaviyo.Services.Content.Models;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Actions;

[ActionList("Campaign variations")]
public class CampaignVariationActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private static readonly string[] SupportedChannels = TranslationChannels.CampaignVariation;
    private readonly IContentService _contentService =
        new ContentServiceFactory(invocationContext, fileManagementClient)
            .GetContentService(TranslationResourceTypes.CampaignVariation);

    [Action("Search campaign variations",
        Description = "Searches translations associated with campaign variations.")]
    public Task<SearchTranslationsResponse> SearchCampaignVariations(
        [ActionParameter] SearchTranslationsRequest input)
    {
        return TranslationSearchHelper.SearchAsync(
            Client, input, [TranslationResourceTypes.CampaignVariation], SupportedChannels);
    }

    [Action("Get campaign variation",
        Description = "Gets campaign variation associated with a translation.")]
    public async Task<CampaignVariationResponse> GetCampaignVariation(
        [ActionParameter] CampaignVariationIdentifier input)
    {
        var (variationId, idChannel) = ParseCampaignVariationId(input.CampaignVariationId);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.CampaignVariation, SupportedChannels, input.Channel, idChannel);
        var translation = await TranslationResourceHelper.FindTranslationAsync(
                              Client, TranslationResourceTypes.CampaignVariation, variationId,
                              SupportedChannels, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"Campaign variation '{variationId}' does not have translations configured" +
                              (requestedChannel is null ? "." : $" for channel '{requestedChannel}'."));
        return await TranslationResourceHelper.GetCampaignVariationByTranslationIdAsync(
            Client, translation.Id);
    }

    [Action("Download campaign variation", Description = "Downloads source or localized campaign variation values as HTML, with campaign variation data as JSON.")]
    public async Task<DownloadCampaignVariationResponse> DownloadCampaignVariation(
        [ActionParameter] DownloadCampaignVariationRequest input)
    {
        var result = await _contentService.DownloadAsync(new ContentDownloadServiceRequest
        {
            ContentId = input.CampaignVariationId,
            Channel = input.Channel,
            Locale = input.Locale
        });

        return new DownloadCampaignVariationResponse
        {
            Content = result.Content,
            JsonFile = result.JsonFile
        };
    }

    [Action("Upload campaign variation", Description = "Uploads translated file to the specified existing or new campaign variation locale.")]
    public Task UploadCampaignVariation(
        [ActionParameter] UploadCampaignVariationRequest input) =>
        _contentService.UploadAsync(new ContentUploadServiceRequest
        {
            ContentId = input.CampaignVariationId,
            Locale = input.Locale,
            SourceLocale = input.SourceLocale,
            Content = input.Content
        });

    private static (string ResourceId, string? Channel) ParseCampaignVariationId(string? variationId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            variationId, TranslationResourceTypes.CampaignVariation, SupportedChannels,
            TranslationResourceDisplayNames.CampaignVariation);

}
