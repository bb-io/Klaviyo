using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.Klaviyo;

[TestClass]
public class FlowMessageFileServiceTests
{
    [TestMethod]
    public void Adding_a_new_locale_preserves_existing_locales()
    {
        var locales = FlowMessageFileService.AddTargetLocale(["de", "fr"], "uk");

        CollectionAssert.AreEqual(new[] { "de", "fr", "uk" }, locales);
        CollectionAssert.AreEqual(new[] { "de", "fr" },
            FlowMessageFileService.AddTargetLocale(["de", "fr"], "FR"));
    }

    [TestMethod]
    public void Flow_message_id_is_resolved_from_downloaded_html_metadata()
    {
        const string flowMessageId = "XLGdyd";
        var metadata = new Dictionary<string, string> { ["FlowMessageId"] = flowMessageId };

        Assert.AreEqual(flowMessageId,
            FlowMessageFileService.ResolveFlowMessageId(null, metadata));
        Assert.AreEqual(flowMessageId,
            FlowMessageFileService.ResolveFlowMessageId(
                $"flow-message::email::{flowMessageId}", metadata));
    }

    [TestMethod]
    public void Flow_message_id_resolution_rejects_mismatched_input_and_metadata()
    {
        var metadata = new Dictionary<string, string> { ["FlowMessageId"] = "XLGdyd" };

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            FlowMessageFileService.ResolveFlowMessageId("another-message", metadata));
    }

    [TestMethod]
    public void Flow_message_id_resolution_requires_input_or_metadata()
    {
        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            FlowMessageFileService.ResolveFlowMessageId(null, new Dictionary<string, string>()));
    }

    [TestMethod]
    public void Translation_html_round_trip_preserves_flow_message_values()
    {
        const string flowMessageId = "XLGdyd";
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string> { ["FlowMessageId"] = flowMessageId },
            new Dictionary<string, string>
            {
                [$"flow_message::{flowMessageId}::subject"] = "Hello & welcome",
                ["BlockType.Text::block-id::data.content"] = "<p>Hello <strong>world</strong></p>",
                ["SubBlockType.Button::button-id::attributes.href"] =
                    "https://example.com/?one=1&two=2"
            });

        var filtered = TemplateHtmlFilterService.Create(html, $"{flowMessageId}.source.html");
        var result = TranslationHtmlFileCodec.Import(filtered);

        Assert.AreEqual(flowMessageId, result.Metadata["FlowMessageId"]);
        Assert.AreEqual("Hello & welcome", result.Values[$"flow_message::{flowMessageId}::subject"]);
        StringAssert.Contains(result.Values["BlockType.Text::block-id::data.content"],
            "<p>Hello <strong>world</strong></p>");
        Assert.AreEqual("https://example.com/?one=1&two=2",
            result.Values["SubBlockType.Button::button-id::attributes.href"]);
    }

    [TestMethod]
    public void Validation_accepts_plain_and_html_flow_message_values()
    {
        var uploadedValues = new Dictionary<string, string>
        {
            ["flow_message::XLGdyd::subject"] = "Hallo!",
            ["BlockType.Text::block-id::data.content"] = "<p>Hallo <strong>Welt</strong></p>"
        };
        TranslationValueDto[] currentValues =
        [
            new()
            {
                Id = "flow_message::XLGdyd::subject",
                SourceValue = "Hello!"
            },
            new()
            {
                Id = "BlockType.Text::block-id::data.content",
                SourceValue = "<p>Hello <strong>world</strong></p>"
            }
        ];

        FlowMessageFileService.ValidateValues(uploadedValues, currentValues, "XLGdyd");
    }

    [TestMethod]
    public void Validation_rejects_value_ids_from_another_flow_message()
    {
        var uploadedValues = new Dictionary<string, string>
        {
            ["flow_message::other::subject"] = "Translated"
        };
        TranslationValueDto[] currentValues =
        [
            new()
            {
                Id = "flow_message::XLGdyd::subject",
                SourceValue = "Source"
            }
        ];

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            FlowMessageFileService.ValidateValues(uploadedValues, currentValues, "XLGdyd"));
    }

    [TestMethod]
    public void Composite_translation_id_is_normalized_to_flow_message_id()
    {
        const string flowMessageId = "XLGdyd";

        Assert.AreEqual(flowMessageId,
            FlowMessageFileService.NormalizeFlowMessageId(
                $"flow-message::email::{flowMessageId}"));
        Assert.AreEqual(flowMessageId,
            FlowMessageFileService.NormalizeFlowMessageId(flowMessageId));
    }
}
