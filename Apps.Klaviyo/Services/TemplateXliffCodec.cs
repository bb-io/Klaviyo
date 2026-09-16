using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Apps.Klaviyo.Api.Dtos;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Bilingual.Xliff1;
using Blackbird.Filters.Bilingual.Xliff2;
using Blackbird.Filters.Coders;
using Blackbird.Filters.Transformations;

namespace Apps.Klaviyo.Services;

public static class TemplateXliffCodec
{
    private static readonly XNamespace XliffNamespace = "urn:oasis:names:tc:xliff:document:2.0";
    private static readonly Regex HtmlTag = new(@"<[/!]?[A-Za-z][^>]*>", RegexOptions.Compiled);

    public static string Export(string translationId, string sourceLocale, string targetLocale,
        IEnumerable<TranslationValueDto> values, string version = "2.2")
    {
        if (version is not ("1.2" or "2.2"))
            throw new PluginMisconfigurationException("XLIFF version must be 1.2 or 2.2.");
        var valueList = values.ToArray();
        var seed = new XElement(XliffNamespace + "xliff",
            new XAttribute("version", "2.0"),
            new XAttribute("srcLang", sourceLocale),
            new XAttribute("trgLang", targetLocale),
            new XElement(XliffNamespace + "file", new XAttribute("id", translationId),
                valueList.Select(value => new XElement(XliffNamespace + "unit",
                    new XAttribute("id", value.Id),
                    new XElement(XliffNamespace + "segment",
                        new XElement(XliffNamespace + "source", value.SourceValue))))));
        var transformation = Xliff2Serializer.Deserialize(seed.ToString(SaveOptions.DisableFormatting));
        transformation.Original = translationId;
        transformation.BilingualFileName = $"{translationId.Split("::").Last()}.{targetLocale}.xlf";
        var units = transformation.GetUnits().ToDictionary(unit => unit.Id
            ?? throw new PluginApplicationException("Filters returned a value without an ID."),
            StringComparer.Ordinal);
        foreach (var value in valueList)
        {
            var coder = HtmlTag.IsMatch(value.SourceValue)
                ? (Blackbird.Filters.Interfaces.ISegmentCoder)new HtmlCoder()
                : new PlaintextCoder();
            if (!units.TryGetValue(value.Id, out var unit))
                throw new PluginApplicationException($"Filters did not preserve value ID '{value.Id}'.");
            unit.SegmentCoder = coder;
            var target = value.Translations.FirstOrDefault(item =>
                string.Equals(item.Key, targetLocale, StringComparison.OrdinalIgnoreCase)).Value;
            var segment = unit.Segments.Single();
            segment.SegmentCoder = coder;
            segment.Source = coder.DeserializeSegment(value.SourceValue);
            segment.Target = coder.DeserializeSegment(target ?? string.Empty);
        }

        return version == "1.2" ? Xliff1Serializer.Serialize(transformation) : transformation.Serialize();
    }

    public static Dictionary<string, string> Import(string xliff, string fileName, string translationId,
        string sourceLocale, string targetLocale, IReadOnlyDictionary<string, TranslationValueDto> currentValues)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xliff));
        var loaded = Transformation.Load(stream, fileName, "application/xliff+xml");
        if (!loaded.Success || !loaded.WasBilingual)
            throw new PluginMisconfigurationException($"Content file is not a supported XLIFF: {loaded.Error}");

        var transformation = loaded.Value;
        if (!string.Equals(transformation.SourceLanguage, sourceLocale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("XLIFF source language does not match Klaviyo.");
        if (!string.Equals(transformation.TargetLanguage, targetLocale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("XLIFF target language does not match Target locale.");
        if (!string.IsNullOrWhiteSpace(transformation.Original) &&
            !string.Equals(transformation.Original, translationId, StringComparison.Ordinal))
            throw new PluginMisconfigurationException("XLIFF file does not match Template ID.");

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var unit in transformation.GetUnits())
        {
            var valueId = unit.Id ?? string.Empty;
            if (!currentValues.TryGetValue(valueId, out var current) || result.ContainsKey(valueId))
                throw new PluginMisconfigurationException($"Unknown or duplicate Klaviyo value ID in XLIFF: '{valueId}'.");
            var source = string.Concat(unit.Segments.SelectMany(segment => segment.Source)
                .Select(element => element.Value));
            if (!string.Equals(NormalizeLineEndings(source), NormalizeLineEndings(current.SourceValue),
                    StringComparison.Ordinal))
                throw new PluginMisconfigurationException(
                    $"Klaviyo source value '{valueId}' changed since Download. Download a fresh file.");
            var target = string.Concat(unit.Segments.SelectMany(segment => segment.Target)
                .Select(element => element.Value));
            if (string.IsNullOrWhiteSpace(target))
                continue;
            TranslationFileCodec.ValidateHtmlTranslation(current.SourceValue, target, valueId);
            result.Add(valueId, target);
        }

        if (result.Count == 0)
            throw new PluginMisconfigurationException("XLIFF does not contain translated target values.");
        return result;
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
