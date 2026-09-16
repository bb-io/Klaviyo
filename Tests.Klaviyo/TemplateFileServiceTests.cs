using Apps.Klaviyo.Services;
using Apps.Klaviyo.Api.Dtos;

namespace Tests.Klaviyo;

[TestClass]
public class TemplateFileServiceTests
{
    [TestMethod]
    public void Adding_a_new_locale_preserves_existing_locales()
    {
        var locales = TemplateFileService.AddTargetLocale(["de", "fr"], "it");

        CollectionAssert.AreEqual(new[] { "de", "fr", "it" }, locales);
        CollectionAssert.AreEqual(new[] { "de", "fr" },
            TemplateFileService.AddTargetLocale(["de", "fr"], "FR"));
    }

    [TestMethod]
    public void Finds_the_html_body_among_multiple_values()
    {
        var body = new TranslationValueDto { Id = "template::abc::body" };
        var result = TemplateFileService.GetHtmlBodyValue(
            [new TranslationValueDto { Id = "template::abc::subject" }, body]);

        Assert.AreSame(body, result);
    }

    [TestMethod]
    public void Filters_create_html_while_preserving_tags_and_variables()
    {
        const string html = "<!DOCTYPE html><html><body><p>Hello {{ person.first_name }}</p></body></html>";

        var result = TemplateHtmlFilterService.Create(html, "template.html");

        StringAssert.Contains(result, "<p>");
        StringAssert.Contains(result, "{{ person.first_name }}");
    }
}
