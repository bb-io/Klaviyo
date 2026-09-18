using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Tests.Klaviyo.Base;

namespace Tests.Klaviyo;

[TestClass]
[TestCategory("Integration")]
public class ActionTests : TestBase
{
    private static readonly DateTime UpdatedFrom = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task Search_templates_works_with_optional_filters()
    {
        var result = await new TemplateActions(InvocationContext, FileManager)
            .SearchTemplates(CreateFilters(TranslationChannels.Email));

        AssertSearchResult(result, TranslationResourceTypes.Template, TranslationChannels.Email);
        Console.WriteLine($"Templates found: {result.TotalCount}");
    }

    [TestMethod]
    public async Task Get_template_works()
    {
        var actions = new TemplateActions(InvocationContext, FileManager);
        var translationId = await ResolveTranslationId(
            "templateTranslationId",
            () => actions.SearchTemplates(new SearchTranslationsRequest()));

        var result = await actions.GetTemplate(new TemplateIdentifier { TemplateId = translationId });

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Id));
    }

    [TestMethod]
    public async Task Download_source_template_returns_html_and_json()
    {
        var actions = new TemplateActions(InvocationContext, FileManager);
        var templateId = await ResolveTemplateId(actions);

        var result = await actions.DownloadTemplate(new DownloadTemplateRequest
        {
            TemplateId = templateId
        });

        Assert.AreEqual("text/html", result.Content.ContentType);
        Assert.AreEqual("application/json", result.JsonFile.ContentType);
        Assert.AreEqual($"{templateId}.source.html", result.Content.Name);
        var html = await FileManager.ReadOutputTextAsync(result.Content);
        StringAssert.Contains(html, $"blackbird-TemplateId");
        StringAssert.Contains(html, $"content=\"{templateId}\"");
        StringAssert.Contains(html, TranslationHtmlFileCodec.TranslationKeyAttribute);
        StringAssert.Contains(await FileManager.ReadOutputTextAsync(result.JsonFile), templateId);
        Console.WriteLine($"Downloaded source HTML: {result.Content.Name}");
        Console.WriteLine($"Downloaded template JSON: {result.JsonFile.Name}");
    }

    [TestMethod]
    public async Task Download_localized_template_returns_html_and_json()
    {
        var actions = new TemplateActions(InvocationContext, FileManager);
        var (templateId, locale) = await ResolveLocalizedTemplate(actions);

        var result = await actions.DownloadTemplate(new DownloadTemplateRequest
        {
            TemplateId = templateId,
            Locale = locale
        });

        Assert.AreEqual("text/html", result.Content.ContentType);
        Assert.AreEqual("application/json", result.JsonFile.ContentType);
        Assert.AreEqual($"{templateId}.{locale}.html", result.Content.Name);
        var html = await FileManager.ReadOutputTextAsync(result.Content);
        StringAssert.Contains(html, $"blackbird-TemplateId");
        StringAssert.Contains(html, $"content=\"{templateId}\"");
        StringAssert.Contains(html, TranslationHtmlFileCodec.TranslationKeyAttribute);
        StringAssert.Contains(await FileManager.ReadOutputTextAsync(result.JsonFile), templateId);
        Console.WriteLine($"Downloaded localized HTML: {result.Content.Name}");
        Console.WriteLine($"Downloaded template JSON: {result.JsonFile.Name}");
    }

    [TestMethod]
    public async Task Search_campaign_variations_works_with_optional_filters()
    {
        var result = await new CampaignVariationActions(InvocationContext, FileManager)
            .SearchCampaignVariations(CreateFilters(TranslationChannels.Email));

        AssertSearchResult(result, TranslationResourceTypes.CampaignVariation, TranslationChannels.Email);
        Console.WriteLine($"Campaign variations found: {result.TotalCount}");
    }

    [TestMethod]
    public async Task Get_campaign_variation_works()
    {
        var actions = new CampaignVariationActions(InvocationContext, FileManager);
        var translationId = await ResolveTranslationId(
            "campaignVariationTranslationId",
            () => actions.SearchCampaignVariations(new SearchTranslationsRequest()));

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
        StringAssert.Contains(html, "blackbird-CampaignVariationId");
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
        StringAssert.Contains(html, "blackbird-CampaignVariationId");
        StringAssert.Contains(html, TranslationHtmlFileCodec.TranslationKeyAttribute);
    }

    [TestMethod]
    public async Task Search_flow_messages_works_with_optional_filters()
    {
        var result = await new FlowMessageActions(InvocationContext, FileManager)
            .SearchFlowMessages(CreateFilters(TranslationChannels.Email));

        AssertSearchResult(result, TranslationResourceTypes.FlowMessage, TranslationChannels.Email);
        Console.WriteLine($"Flow messages found: {result.TotalCount}");
    }

    [TestMethod]
    public async Task Search_universal_content_works_with_optional_filters()
    {
        var result = await new UniversalContentActions(InvocationContext, FileManager)
            .SearchUniversalContent(CreateFilters(TranslationChannels.Email));

        AssertSearchResult(result, TranslationResourceTypes.UniversalContent, TranslationChannels.Email);
        Console.WriteLine($"Universal content items found: {result.TotalCount}");
    }

    [TestMethod]
    public async Task Get_universal_content_works()
    {
        var actions = new UniversalContentActions(InvocationContext, FileManager);
        var translationId = await ResolveTranslationId(
            "universalContentTranslationId",
            () => actions.SearchUniversalContent(new SearchTranslationsRequest()));

        var result = await actions.GetUniversalContent(
            new UniversalContentIdentifier { UniversalContentId = translationId });

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Id));
    }

    [TestMethod]
    public async Task Search_content_works_with_optional_filters()
    {
        var updatedTo = DateTime.UtcNow.AddMinutes(5);
        var result = await new ContentActions(InvocationContext, FileManager).SearchContent(new SearchContentRequest
        {
            ContentTypes =
            [
                TranslationResourceTypes.Template,
                TranslationResourceTypes.FlowMessage
            ],
            Channels = [TranslationChannels.Email],
            UpdatedFrom = UpdatedFrom,
            UpdatedTo = updatedTo
        });

        Assert.IsNotNull(result.Items);
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        Assert.IsTrue(result.Items.All(item =>
            item.ResourceType is TranslationResourceTypes.Template or TranslationResourceTypes.FlowMessage));
        Assert.IsTrue(result.Items.All(item => item.Channel == TranslationChannels.Email));
        Assert.IsTrue(result.Items.All(item => item.Updated >= UpdatedFrom && item.Updated <= updatedTo));
        Console.WriteLine($"Content items found: {result.TotalCount}");
    }

    private static SearchTranslationsRequest CreateFilters(string channel) => new()
    {
        Channels = [channel],
        UpdatedFrom = UpdatedFrom,
        UpdatedTo = DateTime.UtcNow.AddMinutes(5)
    };

    private static void AssertSearchResult(
        SearchTranslationsResponse result,
        string expectedResourceType,
        string expectedChannel)
    {
        Assert.IsNotNull(result.Items);
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        Assert.IsTrue(result.Items.All(item => item.ResourceType == expectedResourceType));
        Assert.IsTrue(result.Items.All(item => item.Channel == expectedChannel));
        Assert.IsTrue(result.Items.All(item => item.Updated >= UpdatedFrom));
    }

    private async Task<string> ResolveTranslationId(
        string configurationKey,
        Func<Task<SearchTranslationsResponse>> search)
    {
        var configuredId = Configuration[$"TestData:{configurationKey}"];
        if (!string.IsNullOrWhiteSpace(configuredId))
            return configuredId;

        var item = (await search()).Items.FirstOrDefault();
        if (item is not null)
            return item.ContentId;

        Assert.Inconclusive(
            $"No translation is available. Set TestData:{configurationKey} in appsettings.json to run this Get test.");
        return string.Empty;
    }

    private async Task<string> ResolveTemplateId(TemplateActions actions)
    {
        var configuredId = Configuration["TestData:templateId"];
        if (!string.IsNullOrWhiteSpace(configuredId))
            return configuredId;

        var configuredTranslationId = Configuration["TestData:templateTranslationId"];
        if (!string.IsNullOrWhiteSpace(configuredTranslationId))
            return configuredTranslationId.Split("::", StringSplitOptions.None).Last();

        var item = (await actions.SearchTemplates(new SearchTranslationsRequest())).Items.FirstOrDefault();
        if (item is not null)
            return item.ResourceId;

        Assert.Inconclusive(
            "No template is available. Set TestData:templateId in appsettings.json to run this Download test.");
        return string.Empty;
    }

    private async Task<(string TemplateId, string Locale)> ResolveLocalizedTemplate(TemplateActions actions)
    {
        var configuredId = Configuration["TestData:templateId"];
        var configuredLocale = Configuration["TestData:templateLocale"];
        if (!string.IsNullOrWhiteSpace(configuredId) && !string.IsNullOrWhiteSpace(configuredLocale))
            return (configuredId, configuredLocale);

        var item = (await actions.SearchTemplates(new SearchTranslationsRequest())).Items
            .FirstOrDefault(template => template.TargetLocales.Any());
        if (item is not null)
            return (item.ResourceId, item.TargetLocales.First());

        Assert.Inconclusive(
            "No localized template is available. Set TestData:templateId and TestData:templateLocale in appsettings.json.");
        return (string.Empty, string.Empty);
    }

    private async Task<(string VariationId, string Locale)> ResolveLocalizedCampaignVariation(
        CampaignVariationActions actions)
    {
        var item = (await actions.SearchCampaignVariations(new SearchTranslationsRequest
            {
                Channels = [TranslationChannels.Email]
            })).Items
            .FirstOrDefault(variation => variation.TargetLocales.Any());
        if (item is not null)
            return (item.ResourceId, item.TargetLocales.First());

        Assert.Inconclusive("No localized email campaign variation is available.");
        return (string.Empty, string.Empty);
    }
}
