using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
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

        var result = await actions.GetTemplate(new TranslationIdentifier { TranslationId = translationId });

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Id));
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
            new TranslationIdentifier { TranslationId = translationId });

        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Id));
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
            new TranslationIdentifier { TranslationId = translationId });

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
}
