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
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Text;

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
            "Campaign variation", SupportedChannels, input.Channel, idChannel);
        var translation = await FindTranslationAsync(variationId, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"Campaign variation '{variationId}' does not have translations configured" +
                              (requestedChannel is null ? "." : $" for channel '{requestedChannel}'."));
        return await ContentActions.GetCampaignVariationAsync(Client, translation.Id);
    }

    [Action("Download campaign variation", Description = "Downloads source or localized campaign variation values as HTML, with campaign variation data as JSON.")]
    public async Task<DownloadCampaignVariationResponse> DownloadCampaignVariation(
        [ActionParameter] DownloadCampaignVariationRequest input)
    {
        var (variationId, idChannel) = ParseCampaignVariationId(input.CampaignVariationId);
        var variation = await GetCampaignVariationAsync(variationId);
        var resourceChannel = GetVariationChannel(variation);
        var requestedChannel = TranslationChannelHelper.ResolveOptionalChannel(
            "Campaign variation", SupportedChannels, input.Channel, idChannel, resourceChannel);
        var translation = await FindTranslationAsync(variationId, requestedChannel)
                          ?? throw new PluginMisconfigurationException(
                              $"Campaign variation '{variationId}' does not have translations configured yet.");
        var channel = TranslationChannelHelper.ResolveChannel(
            "Campaign variation", SupportedChannels, null,
            requestedChannel, translation.Attributes.Channel,
            TranslationChannelHelper.GetChannelFromTranslationId(translation.Id));
        translation = await GetWithValuesAsync(translation.Id);
        if (translation.Attributes.Values.Count == 0)
            throw new PluginApplicationException(
                $"Campaign variation '{variationId}' has no exportable translation values.");

        var locale = TranslationValuesHelper.ResolveDownloadLocale(
            input.Locale, translation.Attributes.TargetLocales, "Campaign variation", variationId);
        var suffix = locale ?? "source";
        var exportedValues = TranslationValuesHelper.SelectExportValues(
            translation.Attributes.Values, locale);
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string>
            {
                ["ContentType"] = TranslationResourceTypes.CampaignVariation,
                ["CampaignVariationId"] = variationId,
                ["Channel"] = channel,
                ["TranslationId"] = translation.Id
            }, exportedValues);
        var fileName = $"{variationId}.{suffix}.html";
        html = TemplateHtmlFilterService.Create(html, fileName);

        var htmlFile = await SaveAsync(html, "text/html", fileName);
        var json = new JObject
        {
            ["data"] = JObject.FromObject(variation),
            ["translation"] = JObject.FromObject(translation)
        };
        var jsonFile = await SaveAsync(json.ToString(), "application/json",
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
            "CampaignVariationId",
            TranslationResourceTypes.CampaignVariation,
            SupportedChannels,
            "Campaign variation");

        var variation = await GetCampaignVariationAsync(variationId);
        var resourceChannel = GetVariationChannel(variation);
        var channel = TranslationChannelHelper.ResolveChannel(
            "Campaign variation", SupportedChannels, null, fileChannel, resourceChannel);
        var existing = await FindTranslationAsync(variationId, channel);
        TranslationDto translation;
        if (existing is null)
        {
            translation = await CreateTranslationAsync(variationId, locale, input.SourceLocale, channel);
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
            htmlFile.Values, current.Attributes.Values, "Campaign variation", variationId);

        var targetLocales = current.Attributes.TargetLocales.Contains(locale, StringComparer.OrdinalIgnoreCase)
            ? null
            : TranslationValuesHelper.AddTargetLocale(current.Attributes.TargetLocales, locale);
        var request = TranslationRequestBuilder.UpdateValues(
            current.Id, htmlFile.Values, locale, targetLocales);
        await Client.ExecuteWithErrorHandling(request);
    }

    private static (string ResourceId, string? Channel) ParseCampaignVariationId(string? variationId) =>
        TranslationChannelHelper.ParseResourceOrTranslationId(
            variationId, TranslationResourceTypes.CampaignVariation, SupportedChannels, "Campaign variation");

    private async Task<RelatedResourceDto> GetCampaignVariationAsync(string variationId)
    {
        var request = new RestRequest("campaign-messages", Method.Get)
            .AddQueryParameter("include", "campaign-variations")
            .AddQueryParameter("page[size]", "100");
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<RelatedResourceDto>>(request);
            var variation = response.Included.FirstOrDefault(item =>
                string.Equals(item.Type, TranslationResourceTypes.CampaignVariation, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Id, variationId, StringComparison.Ordinal));
            if (variation is not null)
                return variation;

            var next = response.Links?.Next;
            if (string.IsNullOrWhiteSpace(next) || !visited.Add(next))
                break;
            request = new RestRequest(next, Method.Get);
        }

        throw new PluginApplicationException(
            $"No campaign variation returned '{variationId}'.");
    }

    private async Task<TranslationDto?> FindTranslationAsync(string variationId, string? channel = null)
    {
        var request = TranslationRequestBuilder.FindByResourceId(variationId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiListResponse<TranslationDto>>(request);
        return response.Data.FirstOrDefault(item =>
            item.Id.StartsWith($"{TranslationResourceTypes.CampaignVariation}::", StringComparison.OrdinalIgnoreCase) &&
            SupportedChannels.Contains(GetChannelFromTranslation(item), StringComparer.OrdinalIgnoreCase) &&
            (channel is null || string.Equals(GetChannelFromTranslation(item), channel,
                StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(item.Relationships[TranslationResourceTypes.CampaignVariation]?["data"]?["id"]?.ToString(),
                variationId, StringComparison.Ordinal));
    }

    private async Task<TranslationDto> GetWithValuesAsync(string translationId)
    {
        var request = TranslationRequestBuilder.GetWithValues(translationId);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"No translation returned '{translationId}'.");
    }

    private async Task<TranslationDto> CreateTranslationAsync(
        string variationId, string locale, string? sourceLocale, string channel)
    {
        sourceLocale = TranslationValuesHelper.ValidateSourceLocale(
            sourceLocale, locale, "Campaign variation");
        var request = TranslationRequestBuilder.Create(
            TranslationResourceTypes.CampaignVariation, variationId, sourceLocale, locale, channel);
        var response = await Client.ExecuteWithErrorHandling<JsonApiSingleResponse<TranslationDto>>(request);
        return response.Data ?? throw new PluginApplicationException(
            $"No translation created for '{variationId}'.");
    }

    private static string GetVariationChannel(RelatedResourceDto variation)
    {
        var channel = variation.Attributes.SelectToken("definition.details.channel")?.ToString()
                      ?? variation.Attributes["channel"]?.ToString();
        return TranslationChannelHelper.ResolveChannel(
            "Campaign variation", SupportedChannels, null, channel);
    }

    private static string? GetChannelFromTranslation(TranslationDto translation) =>
        translation.Attributes.Channel ?? TranslationChannelHelper.GetChannelFromTranslationId(translation.Id);

    private async Task<FileReference> SaveAsync(string content, string mediaType, string fileName) =>
        await fileManagementClient.UploadAsync(
            new MemoryStream(Encoding.UTF8.GetBytes(content)), mediaType, fileName);

}
