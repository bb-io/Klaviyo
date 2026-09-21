using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Identifiers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Tests.Klaviyo.Base;

namespace Tests.Klaviyo;

[TestClass]
public class CampaignVariationActionTests : TestBase
{
    private static readonly DateTime UpdatedFrom = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task Search_campaign_variations_works_with_optional_filters()
    {
        var result = await new CampaignVariationActions(InvocationContext, FileManager)
            .SearchCampaignVariations(CreateFilters());

        Assert.IsNotNull(result.Items);
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        Assert.IsTrue(result.Items.All(item =>
            item.ResourceType == TranslationResourceTypes.CampaignVariation));
        Assert.IsTrue(result.Items.All(item => item.Channel == TranslationChannels.Email));
        Assert.IsTrue(result.Items.All(item => item.Updated >= UpdatedFrom));
    }

    [TestMethod]
    public async Task Get_campaign_variation_works()
    {
        var actions = new CampaignVariationActions(InvocationContext, FileManager);
        var translationId = await ResolveTranslationId(actions);

        var result = await actions.GetCampaignVariation(
            new CampaignVariationIdentifier { CampaignVariationId = translationId });

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Id));
    }

    [TestMethod]
    public async Task Download_source_campaign_variation_returns_html_and_json()
    {
        var actions = new CampaignVariationActions(InvocationContext, FileManager);
        var (variationId, _) = await ResolveLocalizedCampaignVariation(actions);

        var result = await actions.DownloadCampaignVariation(new DownloadCampaignVariationRequest
        {
            CampaignVariationId = variationId
        });

        Assert.AreEqual("text/html", result.Content.ContentType);
        Assert.AreEqual("application/json", result.JsonFile.ContentType);
        Assert.AreEqual($"{variationId}.source.html", result.Content.Name);
        var html = await FileManager.ReadOutputTextAsync(result.Content);
        StringAssert.Contains(html, "blackbird-content-type");
        StringAssert.Contains(html, "blackbird-resource-id");
        StringAssert.Contains(html, "blackbird-channel");
        StringAssert.Contains(html, "blackbird-translation-id");
        StringAssert.Contains(html, $"content=\"{variationId}\"");
        StringAssert.Contains(html, TranslationHtmlFileCodec.TranslationKeyAttribute);
        StringAssert.Contains(await FileManager.ReadOutputTextAsync(result.JsonFile), variationId);
    }

    [TestMethod]
    public async Task Download_localized_campaign_variation_returns_html_and_json()
    {
        var actions = new CampaignVariationActions(InvocationContext, FileManager);
        var (variationId, locale) = await ResolveLocalizedCampaignVariation(actions);

        var result = await actions.DownloadCampaignVariation(new DownloadCampaignVariationRequest
        {
            CampaignVariationId = variationId,
            Locale = locale
        });

        Assert.AreEqual($"{variationId}.{locale}.html", result.Content.Name);
        var html = await FileManager.ReadOutputTextAsync(result.Content);
        StringAssert.Contains(html, "blackbird-content-type");
        StringAssert.Contains(html, "blackbird-resource-id");
        StringAssert.Contains(html, "blackbird-channel");
        StringAssert.Contains(html, "blackbird-translation-id");
        StringAssert.Contains(html, TranslationHtmlFileCodec.TranslationKeyAttribute);
    }

    private static SearchTranslationsRequest CreateFilters() => new()
    {
        Channels = [TranslationChannels.Email],
        UpdatedFrom = UpdatedFrom,
        UpdatedTo = DateTime.UtcNow.AddMinutes(5)
    };

    private async Task<string> ResolveTranslationId(CampaignVariationActions actions)
    {
        var configuredId = Configuration["TestData:campaignVariationTranslationId"];
        if (!string.IsNullOrWhiteSpace(configuredId))
            return configuredId;

        var item = (await actions.SearchCampaignVariations(new SearchTranslationsRequest())).Items.FirstOrDefault();
        if (item is not null)
            return item.ContentId;

        Assert.Inconclusive(
            "No campaign variation translation is available. Set TestData:campaignVariationTranslationId in appsettings.json.");
        return string.Empty;
    }

    private async Task<(string VariationId, string Locale)> ResolveLocalizedCampaignVariation(
        CampaignVariationActions actions)
    {
        var configuredId = Configuration["TestData:campaignVariationId"];
        var configuredLocale = Configuration["TestData:campaignVariationLocale"];
        if (!string.IsNullOrWhiteSpace(configuredId) && !string.IsNullOrWhiteSpace(configuredLocale))
            return (configuredId, configuredLocale);

        var item = (await actions.SearchCampaignVariations(new SearchTranslationsRequest
            {
                Channels = [TranslationChannels.Email]
            })).Items.FirstOrDefault(variation => variation.TargetLocales.Any());
        if (item is not null)
            return (item.ResourceId, item.TargetLocales.First());

        Assert.Inconclusive(
            "No localized campaign variation is available. Set TestData:campaignVariationId and TestData:campaignVariationLocale in appsettings.json.");
        return (string.Empty, string.Empty);
    }
}
