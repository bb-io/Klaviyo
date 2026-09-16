using Apps.Klaviyo.Api.Dtos;
using Apps.Klaviyo.Services;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.Klaviyo;

[TestClass]
public class TemplateXliffCodecTests
{
    private const string TranslationId = "template::email::abc";

    [DataTestMethod]
    [DataRow("1.2")]
    [DataRow("2.2")]
    public void Filters_round_trip_template_values_in_both_xliff_versions(string version)
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

        var xliff = TemplateXliffCodec.Export(TranslationId, "en", "fr", values, version);
        var imported = TemplateXliffCodec.Import(xliff, "abc.fr.xlf", TranslationId, "en", "fr",
            values.ToDictionary(value => value.Id));

        Assert.AreEqual("<p>Bonjour</p>", imported["template::abc::body"]);
        Assert.AreEqual("Bienvenue & bonjour", imported["template::abc::subject"]);
    }

    [TestMethod]
    public void Import_rejects_a_different_target_locale()
    {
        var values = CreateValues();
        var xliff = TemplateXliffCodec.Export(TranslationId, "en", "fr", values);

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            TemplateXliffCodec.Import(xliff, "abc.fr.xlf", TranslationId, "en", "de",
                values.ToDictionary(value => value.Id)));
    }

    [TestMethod]
    public void Import_rejects_stale_source_values()
    {
        var values = CreateValues();
        var xliff = TemplateXliffCodec.Export(TranslationId, "en", "fr", values);
        values[0].SourceValue = "<p>Changed</p>";

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            TemplateXliffCodec.Import(xliff, "abc.fr.xlf", TranslationId, "en", "fr",
                values.ToDictionary(value => value.Id)));
    }

    [TestMethod]
    public void Filters_imports_xliff_2_0()
    {
        var values = CreateValues();
        var xliff = TranslationFileCodec.Export(TranslationId, "en", "fr", values);

        var imported = TemplateXliffCodec.Import(xliff, "abc.fr.xlf", TranslationId, "en", "fr",
            values.ToDictionary(value => value.Id));

        Assert.AreEqual("<p>Bonjour</p>", imported["template::abc::body"]);
    }

    private static TranslationValueDto[] CreateValues() =>
    [
        new TranslationValueDto
        {
            Id = "template::abc::body",
            SourceValue = "<p>Hello</p>",
            Translations = new Dictionary<string, string> { ["fr"] = "<p>Bonjour</p>" }
        }
    ];
}
