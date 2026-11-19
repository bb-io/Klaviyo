using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Services.Content.Models;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using RestSharp;

namespace Apps.Klaviyo.Services.Content.Concrete;

public class CampaignVariationContentService(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient)
    : BaseContentService(invocationContext, fileManagementClient)
{
    private static readonly string[] SupportedChannels = TranslationChannels.CampaignVariation;

    public override async Task<ContentDownloadResult> DownloadAsync(ContentDownloadServiceRequest request)
    {
        var (variationId, idChannel) = ParseCampaignVariationId(request.ContentId);
        var variation = await GetCampaignVariationAsync(variationId);
        var resourceChannel = GetVariationChannel(variation);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.CampaignVariation,
            SupportedChannels,
            request.Channel,
            idChannel,
            resourceChannel);

        return await DownloadTranslatedResourceAsync(
            new ContentDownloadServiceRequest
            {
                ContentId = variationId,
                Channel = request.Channel,
                Locale = request.Locale
            },
            TranslationResourceTypes.CampaignVariation,
            TranslationResourceDisplayNames.CampaignVariation,
            SupportedChannels,
            requestedChannel,
            variation);
    }

    public override async Task UploadAsync(ContentUploadServiceRequest request)
    {
        var (locale, content) = await PrepareUploadAsync(request);
        var (variationId, fileChannel) = TranslationChannelHelper.ResolveUploadResourceReference(
            request.ContentId,
            content.Metadata,
            TranslationResourceTypes.CampaignVariation,
            SupportedChannels,
            TranslationResourceDisplayNames.CampaignVariation);
        var variation = await GetCampaignVariationAsync(variationId);
        var resourceChannel = GetVariationChannel(variation);
        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.CampaignVariation,
            SupportedChannels,
            null,
            fileChannel,
            resourceChannel);

        await TranslationUploadService.UploadAsync(
            Client,
            new TranslationUploadContext(
                TranslationResourceTypes.CampaignVariation,
                TranslationResourceDisplayNames.CampaignVariation,
                variationId,
                SupportedChannels,
                channel,
                locale,
                request.SourceLocale,
                content.Values));
    }

    private async Task<RelatedResourceDto> GetCampaignVariationAsync(string variationId)
    {
        var request = new RestRequest("campaign-messages", Method.Get)
            .AddQueryParameter("include", "campaign-variations");

        await foreach (var response in Client.PaginateAsync<RelatedResourceDto>(request))
        {
            var variation = response.Included.FirstOrDefault(item =>
                string.Equals(
                    item.Type,
                    TranslationResourceTypes.CampaignVariation,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Id, variationId, StringComparison.Ordinal));
            if (variation is not null)
                return variation;
        }

        throw new PluginApplicationException($"No campaign variation returned '{variationId}'.");
    }

    private static string GetVariationChannel(RelatedResourceDto variation)
    {
        var channel = variation.Attributes.SelectToken("definition.details.channel")?.ToString()
                      ?? variation.Attributes["channel"]?.ToString();
        return TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.CampaignVariation,
            SupportedChannels,
            null,
            channel);
    }

    private static (string ResourceId, string? Channel) ParseCampaignVariationId(string? variationId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            variationId,
            TranslationResourceTypes.CampaignVariation,
            SupportedChannels,
            TranslationResourceDisplayNames.CampaignVariation);
}
