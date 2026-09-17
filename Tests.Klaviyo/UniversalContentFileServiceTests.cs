using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.Klaviyo;

[TestClass]
public class UniversalContentFileServiceTests
{
    [TestMethod]
    public void Adding_a_new_locale_preserves_existing_locales()
    {
        var locales = UniversalContentFileService.AddTargetLocale(["de", "fr"], "uk");

        CollectionAssert.AreEqual(new[] { "de", "fr", "uk" }, locales);
        CollectionAssert.AreEqual(new[] { "de", "fr" },
            UniversalContentFileService.AddTargetLocale(["de", "fr"], "FR"));
    }

    [TestMethod]
    public void Universal_content_id_is_resolved_from_downloaded_html_metadata()
    {
        const string universalContentId = "d13f5e0b8eb74e6ba2f2a3aff214fc02";
        var metadata = new Dictionary<string, string>
        {
            ["UniversalContentId"] = universalContentId
        };

        Assert.AreEqual(universalContentId,
            UniversalContentFileService.ResolveUniversalContentId(null, metadata));
        Assert.AreEqual(universalContentId,
            UniversalContentFileService.ResolveUniversalContentId(
                $"template-universal-content::email::{universalContentId}", metadata));
    }

    [TestMethod]
    public void Universal_content_id_resolution_rejects_mismatched_input_and_metadata()
    {
        var metadata = new Dictionary<string, string>
        {
            ["UniversalContentId"] = "d13f5e0b8eb74e6ba2f2a3aff214fc02"
        };

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            UniversalContentFileService.ResolveUniversalContentId("another-content", metadata));
    }

    [TestMethod]
    public void Universal_content_id_resolution_requires_input_or_metadata()
    {
        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            UniversalContentFileService.ResolveUniversalContentId(
                null, new Dictionary<string, string>()));
    }

    [TestMethod]
    public void Translation_html_round_trip_preserves_universal_content_values()
    {
        const string universalContentId = "d13f5e0b8eb74e6ba2f2a3aff214fc02";
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string> { ["UniversalContentId"] = universalContentId },
            new Dictionary<string, string>
            {
                ["BlockType.Text::block-id::data.content"] =
                    "<div><h1>Welcome</h1><p>Hello <strong>world</strong></p></div>",
                ["BlockType.Button::button-id::data.content"] = "Explore",
                ["BlockType.Button::button-id::data.attributes.href"] =
                    "https://example.com/?one=1&two=2"
            });

        var filtered = TemplateHtmlFilterService.Create(html, $"{universalContentId}.source.html");
        var result = TranslationHtmlFileCodec.Import(filtered);

        Assert.AreEqual(universalContentId, result.Metadata["UniversalContentId"]);
        StringAssert.Contains(result.Values["BlockType.Text::block-id::data.content"],
            "<h1>Welcome</h1>");
        Assert.AreEqual("Explore", result.Values["BlockType.Button::button-id::data.content"]);
        Assert.AreEqual("https://example.com/?one=1&two=2",
            result.Values["BlockType.Button::button-id::data.attributes.href"]);
    }

    [TestMethod]
    public void Validation_accepts_plain_and_html_universal_content_values()
    {
        var uploadedValues = new Dictionary<string, string>
        {
            ["BlockType.Text::block-id::data.content"] = "<p>Hallo <strong>Welt</strong></p>",
            ["BlockType.Button::button-id::data.content"] = "Erkunden"
        };
        TranslationValueDto[] currentValues =
        [
            new()
            {
                Id = "BlockType.Text::block-id::data.content",
                SourceValue = "<p>Hello <strong>world</strong></p>"
            },
            new()
            {
                Id = "BlockType.Button::button-id::data.content",
                SourceValue = "Explore"
            }
        ];

        UniversalContentFileService.ValidateValues(
            uploadedValues, currentValues, "universal-content-id");
    }

    [TestMethod]
    public void Validation_rejects_value_ids_from_another_universal_content()
    {
        var uploadedValues = new Dictionary<string, string>
        {
            ["BlockType.Text::other-block::data.content"] = "Translated"
        };
        TranslationValueDto[] currentValues =
        [
            new()
            {
                Id = "BlockType.Text::block-id::data.content",
                SourceValue = "Source"
            }
        ];

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            UniversalContentFileService.ValidateValues(
                uploadedValues, currentValues, "universal-content-id"));
    }

    [TestMethod]
    public void Composite_translation_id_is_normalized_to_universal_content_id()
    {
        const string universalContentId = "d13f5e0b8eb74e6ba2f2a3aff214fc02";

        Assert.AreEqual(universalContentId,
            UniversalContentFileService.NormalizeUniversalContentId(
                $"template-universal-content::email::{universalContentId}"));
        Assert.AreEqual(universalContentId,
            UniversalContentFileService.NormalizeUniversalContentId(universalContentId));
    }
}
