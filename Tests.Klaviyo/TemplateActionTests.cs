using Apps.Klaviyo.Actions;
using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Models.Identifiers;
using Apps.Klaviyo.Models.Requests;
using Apps.Klaviyo.Models.Responses;
using Apps.Klaviyo.Services;
using Tests.Klaviyo.Base;

namespace Tests.Klaviyo;

[TestClass]
public class TemplateActionTests : TestBase
{
    private static readonly DateTime UpdatedFrom = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task Search_templates_works_with_optional_filters()
    {
        var result = await new TemplateActions(InvocationContext, FileManager)
            .SearchTemplates(CreateFilters());

        AssertSearchResult(result);
        Console.WriteLine($"Templates found: {result.TotalCount}");
    }

    [TestMethod]
    public async Task Get_template_works()
    {
        var actions = new TemplateActions(InvocationContext, FileManager);
        var translationId = await ResolveTranslationId(actions);

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
        StringAssert.Contains(html, "blackbird-content-type");
        StringAssert.Contains(html, "blackbird-resource-id");
        StringAssert.Contains(html, "blackbird-channel");
        StringAssert.Contains(html, $"content=\"{templateId}\"");
        StringAssert.Contains(html, TranslationHtmlFileCodec.TranslationKeyAttribute);
        StringAssert.Contains(await FileManager.ReadOutputTextAsync(result.JsonFile), templateId);
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
        StringAssert.Contains(html, "blackbird-content-type");
        StringAssert.Contains(html, "blackbird-resource-id");
        StringAssert.Contains(html, "blackbird-channel");
        StringAssert.Contains(html, "blackbird-translation-id");
        StringAssert.Contains(html, $"content=\"{templateId}\"");
        StringAssert.Contains(html, TranslationHtmlFileCodec.TranslationKeyAttribute);
        StringAssert.Contains(await FileManager.ReadOutputTextAsync(result.JsonFile), templateId);
    }

    private static SearchTranslationsRequest CreateFilters() => new()
    {
        Channels = [TranslationChannels.Email],
        UpdatedFrom = UpdatedFrom,
        UpdatedTo = DateTime.UtcNow.AddMinutes(5)
    };

    private static void AssertSearchResult(SearchTranslationsResponse result)
    {
        Assert.IsNotNull(result.Items);
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        Assert.IsTrue(result.Items.All(item => item.ResourceType == TranslationResourceTypes.Template));
        Assert.IsTrue(result.Items.All(item => item.Channel == TranslationChannels.Email));
        Assert.IsTrue(result.Items.All(item => item.Updated >= UpdatedFrom));
    }

    private async Task<string> ResolveTranslationId(TemplateActions actions)
    {
        var configuredId = Configuration["TestData:templateTranslationId"];
        if (!string.IsNullOrWhiteSpace(configuredId))
            return configuredId;

        var item = (await actions.SearchTemplates(new SearchTranslationsRequest())).Items.FirstOrDefault();
        if (item is not null)
            return item.ContentId;

        Assert.Inconclusive(
            "No template translation is available. Set TestData:templateTranslationId in appsettings.json.");
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

        Assert.Inconclusive("No template is available. Set TestData:templateId in appsettings.json.");
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
}
