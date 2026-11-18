using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Models.Identifiers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Klaviyo.Actions;

[ActionList("Campaign variations")]
public class CampaignVariationActions(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    private static readonly string[] SupportedChannels = TranslationChannels.CampaignVariation;

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
        var (variationId, idChannel) = ParseCampaignVariationId(input.CampaignVariationId);
        var variation = await GetCampaignVariationAsync(variationId);
        var resourceChannel = GetVariationChannel(variation);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            TranslationResourceDisplayNames.CampaignVariation,
            SupportedChannels, input.Channel, idChannel, resourceChannel);
        var translation = await TranslationResourceHelper.FindTranslationAsync(
                              Client, TranslationResourceTypes.CampaignVariation, variationId,
                              SupportedChannels, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"Campaign variation '{variationId}' does not have translations configured yet.");
        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.CampaignVariation, SupportedChannels, null,
            requestedChannel, translation.Attributes.Channel,
            TranslationChannelHelper.GetChannelFromTranslationId(translation.Id));
        translation = await TranslationResourceHelper.GetWithValuesAsync(Client, translation.Id);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"Campaign variation '{variationId}' has no exportable translation values.");

        var locale = TranslationValuesHelper.ResolveDownloadLocale(
            input.Locale, translation.Attributes.TargetLocales,
            TranslationResourceDisplayNames.CampaignVariation, variationId);
        var suffix = locale ?? "source";
        var exportedValues = TranslationValuesHelper.SelectExportValues(
            translation.Attributes.Values, locale);
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string>
            {
                [TranslationMetadataKeys.ContentType] = TranslationResourceTypes.CampaignVariation,
                [TranslationMetadataKeys.ResourceId] = variationId,
                [TranslationMetadataKeys.Channel] = channel,
                [TranslationMetadataKeys.TranslationId] = translation.Id
            }, exportedValues);
        var fileName = $"{variationId}.{suffix}.html";
        html = TemplateHtmlFilterService.Create(html, fileName);

        var htmlFile = await TranslationFileService.UploadTextAsync(
            fileManagementClient, html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(variation),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await TranslationFileService.UploadTextAsync(fileManagementClient,
            json.ToString(), "application/json",
            $"{variationId}.{suffix}.json");

        return new DownloadCampaignVariationResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    [Action("Upload campaign variation", Description = "Uploads translated file to the specified existing or new campaign variation locale.")]
    public Task UploadCampaignVariation(
        [ActionParameter] UploadCampaignVariationRequest input) =>
        UploadCampaignVariation(input, null);

    internal async Task UploadCampaignVariation(
        UploadCampaignVariationRequest input,
        TranslationHtmlFile? htmlFile)
    {
        var locale = TranslationValuesHelper.ValidateTargetLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        htmlFile ??= await TranslationFileService.ReadAsync(fileManagementClient, input.Content);
        var (variationId, fileChannel) = TranslationChannelHelper.ResolveUploadResourceReference(
            input.CampaignVariationId,
            htmlFile.Metadata,
            TranslationResourceTypes.CampaignVariation,
            SupportedChannels,
            TranslationResourceDisplayNames.CampaignVariation);

        var variation = await GetCampaignVariationAsync(variationId);
        var resourceChannel = GetVariationChannel(variation);
        var channel = TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.CampaignVariation,
            SupportedChannels, null, fileChannel, resourceChannel);
        await TranslationUploadService.UploadAsync(
            Client,
            new TranslationUploadContext(
                TranslationResourceTypes.CampaignVariation,
                TranslationResourceDisplayNames.CampaignVariation,
                variationId,
                SupportedChannels,
                channel,
                locale,
                input.SourceLocale,
                htmlFile.Values));
    }

    private static (string ResourceId, string? Channel) ParseCampaignVariationId(string? variationId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            variationId, TranslationResourceTypes.CampaignVariation, SupportedChannels,
            TranslationResourceDisplayNames.CampaignVariation);

    private async Task<RelatedResourceDto> GetCampaignVariationAsync(string variationId)
    {
        var request = new RestRequest("campaign-messages", Method.Get)
            .AddQueryParameter("include", "campaign-variations");

        await foreach (var response in Client.PaginateAsync<RelatedResourceDto>(request))
        {
            var variation = response.Included.FirstOrDefault(item =>
                string.Equals(item.Type, TranslationResourceTypes.CampaignVariation, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Id, variationId, StringComparison.Ordinal));
            if (variation is not null)
                return variation;
        }

        throw new PluginApplicationException(
            $"No campaign variation returned '{variationId}'.");
    }

    private static string GetVariationChannel(RelatedResourceDto variation)
    {
        var channel = variation.Attributes.SelectToken("definition.details.channel")?.ToString()
                      ?? variation.Attributes["channel"]?.ToString();
        return TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.CampaignVariation, SupportedChannels, null, channel);
    }

}
