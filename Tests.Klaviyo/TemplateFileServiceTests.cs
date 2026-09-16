using Apps.Klaviyo.Services;
using Apps.Klaviyo.Api.Dtos;
using Blackbird.Applications.Sdk.Common.Exceptions;

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
    public void Translation_html_round_trip_preserves_metadata_and_multiple_values()
    {
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string> { ["TemplateId"] = "abc" },
            new Dictionary<string, string>
            {
                ["template::abc::subject"] = "Hello & welcome",
                ["template::abc::body"] = "<!DOCTYPE html><html><head><title>Ignored</title></head><body><p>Hello <strong>world</strong></p></body></html>"
            });

        var result = TranslationHtmlFileCodec.Import(html);

        Assert.AreEqual("abc", result.Metadata["TemplateId"]);
        Assert.AreEqual("Hello & welcome", result.Values["template::abc::subject"]);
        StringAssert.Contains(result.Values["template::abc::body"], "<p>Hello <strong>world</strong></p>");
        Assert.IsFalse(result.Values["template::abc::body"].Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Values["template::abc::body"].Contains("<html", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Filters_create_html_while_preserving_tags_and_variables()
    {
        const string html = "<!DOCTYPE html><html><body><p>Hello {{ person.first_name }}</p></body></html>";

        var result = TemplateHtmlFilterService.Create(html, "template.html");

        StringAssert.Contains(result, "<p>");
        StringAssert.Contains(result, "{{ person.first_name }}");
    }

    [TestMethod]
    public void Filters_preserve_translation_value_ids_and_template_metadata()
    {
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string> { ["TemplateId"] = "abc" },
            new Dictionary<string, string>
            {
                ["template::abc::body"] = "<p>Hello {{ person.first_name }}</p>"
            });

        var filtered = TemplateHtmlFilterService.Create(html, "template.html");
        var result = TranslationHtmlFileCodec.Import(filtered);

        Assert.AreEqual("abc", result.Metadata["TemplateId"]);
        StringAssert.Contains(result.Values["template::abc::body"], "{{ person.first_name }}");
        StringAssert.Contains(result.Values["template::abc::body"], "<p>");
    }

    [TestMethod]
    public void Import_rejects_duplicate_translation_value_ids()
    {
        const string html = """
                            <html><body>
                            <div data-klaviyo-translation-key="template::abc::body">First</div>
                            <div data-klaviyo-translation-key="template::abc::body">Second</div>
                            </body></html>
                            """;

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            TranslationHtmlFileCodec.Import(html));
    }

    [TestMethod]
    public void Validation_rejects_value_ids_from_another_template()
    {
        var uploadedValues = new Dictionary<string, string>
        {
            ["template::other::body"] = "<p>Translated</p>"
        };
        TranslationValueDto[] currentValues =
        [
            new()
            {
                Id = "template::abc::body",
                SourceValue = "<p>Source</p>"
            }
        ];

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            TemplateFileService.ValidateValueIds(uploadedValues, currentValues, "abc"));
    }

    [TestMethod]
    public void Validation_accepts_a_body_fragment_for_a_full_source_document()
    {
        var uploadedValues = new Dictionary<string, string>
        {
            ["template::abc::body"] = "<h1>Hallo!</h1><p>Übersetzter Inhalt.</p>"
        };
        TranslationValueDto[] currentValues =
        [
            new()
            {
                Id = "template::abc::body",
                SourceValue = "<!DOCTYPE html><html><head></head><body><h1>Hello!</h1><p>Source content.</p></body></html>"
            }
        ];

        TemplateFileService.ValidateValues(uploadedValues, currentValues, "abc");
    }
}
