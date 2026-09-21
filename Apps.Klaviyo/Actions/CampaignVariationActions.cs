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
    public async Task<SearchTranslationsResponse> SearchCampaignVariations(
        [ActionParameter] SearchTranslationsRequest input)
    {
        DateRangeValidator.Validate(input.UpdatedFrom, input.UpdatedTo);
        var selectedChannels = TranslationSearchHelper.NormalizeAndValidate(
            input.Channels, SupportedChannels, "channel");
        string[] resourceTypes = [TranslationResourceTypes.CampaignVariation];
        var request = new RestRequest("translations", Method.Get)
            .AddQueryParameter("include", TranslationResourceTypes.CampaignVariation);

        var items = new List<TranslationResponse>();
        await foreach (var response in Client.PaginateAsync<TranslationDto>(request))
        {
            var included = response.Included
                .GroupBy(item => $"{item.Type}::{item.Id}")
                .ToDictionary(group => group.Key, group => group.First());
            items.AddRange(response.Data
                .Select(item => TranslationSearchHelper.MapTranslation(item, included))
                .Where(item => TranslationSearchHelper.Matches(
                    item, resourceTypes, selectedChannels, input)));
        }

        return new SearchTranslationsResponse
        {
            Items = items.OrderByDescending(item => item.Updated ?? item.Created ?? DateTime.MinValue).ToList()
        };
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
        translation = await GetWithValuesAsync(translation.Id);
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

        var htmlFile = await TranslationResourceHelper.SaveAsync(
            fileManagementClient, html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(variation),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await TranslationResourceHelper.SaveAsync(fileManagementClient,
            json.ToString(), "application/json",
            $"{variationId}.{suffix}.json");

        return new DownloadCampaignVariationResponse
        {
            Content = htmlFile,
            JsonFile = jsonFile
        };
    }

    [Action("Upload campaign variation", Description = "Uploads translated file to the specified existing or new campaign variation locale.")]
    public async Task UploadCampaignVariation(
        [ActionParameter] UploadCampaignVariationRequest input)
    {
        var locale = TranslationValuesHelper.ValidateTargetLocale(input.Locale);
        if (input.Content is null)
            throw new PluginMisconfigurationException("Content file is required.");

        using var stream = await fileManagementClient.DownloadAsync(input.Content);
        var htmlFile = await TranslationContentReader.ReadAsync(stream, input.Content.Name);
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
        var existing = await TranslationResourceHelper.FindTranslationAsync(
            Client, TranslationResourceTypes.CampaignVariation, variationId, SupportedChannels, channel);
        TranslationDto translation;
        if (existing is null)
        {
            translation = await TranslationResourceHelper.CreateTranslationAsync(
                Client, TranslationResourceTypes.CampaignVariation,
                TranslationResourceDisplayNames.CampaignVariation,
                variationId, locale, input.SourceLocale, channel);
        }
        else
        {
            if (string.Equals(existing.Attributes.SourceLocale, locale, StringComparison.OrdinalIgnoreCase))
                throw new PluginMisconfigurationException("Target locale must differ from source locale.");
            locale = existing.Attributes.TargetLocales.FirstOrDefault(value =>
                         string.Equals(value, locale, StringComparison.OrdinalIgnoreCase)) ?? locale;
            translation = existing;
        }

        var current = await GetWithValuesAsync(translation.Id);
        TranslationValuesHelper.ValidateValueIds(
            htmlFile.Values, current.Attributes.Values,
            TranslationResourceDisplayNames.CampaignVariation, variationId);

        var targetLocales = current.Attributes.TargetLocales.Contains(locale, StringComparer.OrdinalIgnoreCase)
            ? null
            : TranslationValuesHelper.AddTargetLocale(current.Attributes.TargetLocales, locale);
        var request = TranslationRequestBuilder.UpdateValues(
            current.Id, htmlFile.Values, locale, targetLocales);
        await Client.ExecuteWithErrorHandling(request);
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

    private async Task<TranslationDto> GetWithValuesAsync(string translationId)
    {
        var request = TranslationRequestBuilder.GetWithValues(translationId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"No translation returned '{translationId}'.");
    }

    private static string GetVariationChannel(RelatedResourceDto variation)
    {
        var channel = variation.Attributes.SelectToken("definition.details.channel")?.ToString()
                      ?? variation.Attributes["channel"]?.ToString();
        return TranslationChannelHelper.ResolveChannel(
            TranslationResourceDisplayNames.CampaignVariation, SupportedChannels, null, channel);
    }

}
