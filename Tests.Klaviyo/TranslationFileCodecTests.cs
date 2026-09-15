using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common.Exceptions;
using System.Xml.Linq;

namespace Tests.Klaviyo;

[TestClass]
public class TranslationFileCodecTests
{
    private const string TranslationId = "template::email::U7pVWU";
    private const string BodyId = "template::U7pVWU::body";
    private const string SourceHtml = "<!DOCTYPE html><html><body><h1>Hello!</h1></body></html>";
    private const string TargetHtml = "<!DOCTYPE html><html><body><h1>Hallo!</h1></body></html>";

    [TestMethod]
    public void Export_and_import_preserve_value_ids_and_html()
    {
        var values = new[]
        {
            new TranslationValueDto
            {
                Id = BodyId,
                SourceValue = SourceHtml,
                Translations = new Dictionary<string, string> { ["de"] = TargetHtml }
            },
            new TranslationValueDto
            {
                Id = "template::U7pVWU::subject",
                SourceValue = "Hello",
                Translations = new Dictionary<string, string> { ["de"] = "Hallo" }
            }
        };

        var xliff = TranslationFileCodec.Export(TranslationId, "en", "de", values);
        var result = TranslationFileCodec.Import(xliff, TranslationId, "en", "de",
            values.ToDictionary(value => value.Id));

        Assert.AreEqual(TargetHtml, result[BodyId]);
        Assert.AreEqual("Hallo", result["template::U7pVWU::subject"]);
    }

    [TestMethod]
    public void Import_rejects_html_with_missing_tags()
    {
        var value = new TranslationValueDto { Id = BodyId, SourceValue = SourceHtml };
        var xliff = TranslationFileCodec.Export(TranslationId, "en", "de", [value]);
        var document = XDocument.Parse(xliff);
        document.Descendants().Single(element => element.Name.LocalName == "target").Value = "Hallo!";

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            TranslationFileCodec.Import(document.ToString(), TranslationId, "en", "de",
                new Dictionary<string, TranslationValueDto> { [BodyId] = value }));
    }

    [TestMethod]
    public void Import_rejects_mismatched_locale()
    {
        var value = new TranslationValueDto { Id = BodyId, SourceValue = SourceHtml };
        var xliff = TranslationFileCodec.Export(TranslationId, "en", "de", [value]);

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            TranslationFileCodec.Import(xliff, TranslationId, "en", "fr",
                new Dictionary<string, TranslationValueDto> { [BodyId] = value }));
    }

    [TestMethod]
    public void Import_rejects_stale_source()
    {
        var value = new TranslationValueDto
        {
            Id = BodyId,
            SourceValue = SourceHtml,
            Translations = new Dictionary<string, string> { ["de"] = TargetHtml }
        };
        var xliff = TranslationFileCodec.Export(TranslationId, "en", "de", [value]);
        value.SourceValue = "<html><body>Changed</body></html>";

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            TranslationFileCodec.Import(xliff, TranslationId, "en", "de",
                new Dictionary<string, TranslationValueDto> { [BodyId] = value }));
    }

    [TestMethod]
    public void Export_and_import_preserve_multiline_text_and_entities()
    {
        var value = new TranslationValueDto
        {
            Id = "flow-message::abc::body",
            SourceValue = "Hello & welcome!\nSecond line.",
            Translations = new Dictionary<string, string> { ["fr"] = "Bonjour & bienvenue !\nDeuxième ligne." }
        };
        var xliff = TranslationFileCodec.Export("flow-message::email::abc", "en", "fr", [value]);

        var result = TranslationFileCodec.Import(xliff, "flow-message::email::abc", "en", "fr",
            new Dictionary<string, TranslationValueDto> { [value.Id] = value });

        Assert.AreEqual(value.Translations["fr"], result[value.Id]);
    }

    [TestMethod]
    public void Export_only_includes_selected_target_locale()
    {
        var value = new TranslationValueDto
        {
            Id = "template::U7pVWU::subject",
            SourceValue = "Hello",
            Translations = new Dictionary<string, string>
            {
                ["de"] = "Hallo",
                ["fr"] = "Bonjour"
            }
        };

        var xliff = TranslationFileCodec.Export(TranslationId, "en", "fr", [value]);
        var document = XDocument.Parse(xliff);

        Assert.AreEqual("fr", (string?)document.Root?.Attribute("trgLang"));
        Assert.AreEqual("Bonjour", document.Descendants().Single(element => element.Name.LocalName == "target").Value);
        Assert.IsFalse(xliff.Contains("Hallo", StringComparison.Ordinal));
    }
}
