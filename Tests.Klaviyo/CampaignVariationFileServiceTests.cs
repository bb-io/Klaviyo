using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.Klaviyo;

[TestClass]
public class CampaignVariationFileServiceTests
{
    [TestMethod]
    public void Adding_a_new_locale_preserves_existing_locales()
    {
        var locales = CampaignVariationFileService.AddTargetLocale(["de", "fr"], "uk");

        CollectionAssert.AreEqual(new[] { "de", "fr", "uk" }, locales);
        CollectionAssert.AreEqual(new[] { "de", "fr" },
            CampaignVariationFileService.AddTargetLocale(["de", "fr"], "FR"));
    }

    [TestMethod]
    public void Campaign_variation_id_is_resolved_from_downloaded_html_metadata()
    {
        const string variationId = "01M2GDND3A24Z8Q6T5QS9R7KBV";
        var metadata = new Dictionary<string, string> { ["CampaignVariationId"] = variationId };

        Assert.AreEqual(variationId,
            CampaignVariationFileService.ResolveCampaignVariationId(null, metadata));
        Assert.AreEqual(variationId,
            CampaignVariationFileService.ResolveCampaignVariationId(
                $"campaign-variation::email::{variationId}", metadata));
    }

    [TestMethod]
    public void Campaign_variation_id_resolution_rejects_mismatched_input_and_metadata()
    {
        var metadata = new Dictionary<string, string>
        {
            ["CampaignVariationId"] = "01M2GDND3A24Z8Q6T5QS9R7KBV"
        };

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            CampaignVariationFileService.ResolveCampaignVariationId("another-variation", metadata));
    }

    [TestMethod]
    public void Campaign_variation_id_resolution_requires_input_or_metadata()
    {
        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            CampaignVariationFileService.ResolveCampaignVariationId(
                null, new Dictionary<string, string>()));
    }

    [TestMethod]
    public void Translation_html_round_trip_preserves_campaign_values()
    {
        const string variationId = "01M2GDND3A24Z8Q6T5QS9R7KBV";
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string> { ["CampaignVariationId"] = variationId },
            new Dictionary<string, string>
            {
                [$"scheduled_message::{variationId}::subject"] = "Hello & welcome",
                ["BlockType.Text::block-id::data.content"] = "<p>Hello <strong>world</strong></p>",
                ["SubBlockType.SocialLinkIcon::link-id::attributes.link_url"] =
                    "https://example.com/?one=1&two=2"
            });

        var filtered = TemplateHtmlFilterService.Create(html, $"{variationId}.source.html");
        var result = TranslationHtmlFileCodec.Import(filtered);

        Assert.AreEqual(variationId, result.Metadata["CampaignVariationId"]);
        Assert.AreEqual("Hello & welcome", result.Values[$"scheduled_message::{variationId}::subject"]);
        StringAssert.Contains(result.Values["BlockType.Text::block-id::data.content"],
            "<p>Hello <strong>world</strong></p>");
        Assert.AreEqual("https://example.com/?one=1&two=2",
            result.Values["SubBlockType.SocialLinkIcon::link-id::attributes.link_url"]);
    }

    [TestMethod]
    public void Validation_accepts_plain_and_html_campaign_values()
    {
        var uploadedValues = new Dictionary<string, string>
        {
            ["scheduled_message::variation-id::subject"] = "Hallo!",
            ["BlockType.Text::block-id::data.content"] = "<p>Hallo <strong>Welt</strong></p>"
        };
        TranslationValueDto[] currentValues =
        [
            new()
            {
                Id = "scheduled_message::variation-id::subject",
                SourceValue = "Hello!"
            },
            new()
            {
                Id = "BlockType.Text::block-id::data.content",
                SourceValue = "<p>Hello <strong>world</strong></p>"
            }
        ];

        CampaignVariationFileService.ValidateValues(uploadedValues, currentValues, "variation-id");
    }

    [TestMethod]
    public void Validation_rejects_value_ids_from_another_campaign_variation()
    {
        var uploadedValues = new Dictionary<string, string>
        {
            ["scheduled_message::other::subject"] = "Translated"
        };
        TranslationValueDto[] currentValues =
        [
            new()
            {
                Id = "scheduled_message::variation-id::subject",
                SourceValue = "Source"
            }
        ];

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            CampaignVariationFileService.ValidateValues(uploadedValues, currentValues, "variation-id"));
    }

    [TestMethod]
    public void Composite_translation_id_is_normalized_to_variation_id()
    {
        const string variationId = "01M2GDND3A24Z8Q6T5QS9R7KBV";

        Assert.AreEqual(variationId,
            CampaignVariationFileService.NormalizeCampaignVariationId(
                $"campaign-variation::email::{variationId}"));
        Assert.AreEqual(variationId,
            CampaignVariationFileService.NormalizeCampaignVariationId(variationId));
    }
}
