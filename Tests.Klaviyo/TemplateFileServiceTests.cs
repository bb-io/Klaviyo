using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Services;
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
    public void Html_translation_preserves_multiple_value_ids_and_translations()
    {
        var values = new[]
        {
            new TranslationValueDto
            {
                Id = "template::abc::body",
                SourceValue = "<p>Hello</p>",
                Translations = new Dictionary<string, string> { ["fr"] = "<p>Bonjour</p>" }
            },
            new TranslationValueDto
            {
                Id = "template::abc::subject",
                SourceValue = "Welcome & hello",
                Translations = new Dictionary<string, string> { ["fr"] = "Bienvenue & bonjour" }
            }
        };

        var html = TemplateHtmlCodec.Export("abc", "fr", values);
        var imported = TemplateHtmlCodec.Import(html, "abc", values);

        Assert.AreEqual("<p>Bonjour</p>", imported["template::abc::body"]);
        Assert.AreEqual("Bienvenue & bonjour", imported["template::abc::subject"]);
    }

    [TestMethod]
    public void Html_translation_rejects_missing_value_ids()
    {
        var values = new[]
        {
            new TranslationValueDto { Id = "body", SourceValue = "<p>Hello</p>" },
            new TranslationValueDto { Id = "subject", SourceValue = "Welcome" }
        };
        var html = TemplateHtmlCodec.Export("abc", null, values);
        html = html.Replace("data-klaviyo-value-id=\"subject\"", "data-klaviyo-value-id=\"other\"");

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            TemplateHtmlCodec.Import(html, "abc", values));
    }

    [TestMethod]
    public void Html_translation_preserves_full_document_value_with_other_values()
    {
        var values = new[]
        {
            new TranslationValueDto
            {
                Id = "body", SourceValue = "<!DOCTYPE html><html><body><p>Hello</p></body></html>"
            },
            new TranslationValueDto { Id = "subject", SourceValue = "Welcome" }
        };
        var html = TemplateHtmlCodec.Export("abc", null, values);

        var imported = TemplateHtmlCodec.Import(html, "abc", values);

        Assert.AreEqual(values[0].SourceValue, imported["body"]);
    }

    [TestMethod]
    public void Html_translation_rejects_stale_source_values()
    {
        var values = new[]
        {
            new TranslationValueDto { Id = "body", SourceValue = "<p>Hello</p>" },
            new TranslationValueDto { Id = "subject", SourceValue = "Welcome" }
        };
        var html = TemplateHtmlCodec.Export("abc", null, values);
        values[1].SourceValue = "Updated welcome";

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            TemplateHtmlCodec.Import(html, "abc", values));
    }
}
