using System.Text.RegularExpressions;
using System.Xml.Linq;
using Apps.Klaviyo.Api.Dtos;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Bilingual.Xliff2;

namespace Apps.Klaviyo.Services;

public static class TranslationFileCodec
{
    private static readonly XNamespace XliffNamespace = "urn:oasis:names:tc:xliff:document:2.0";
    private static readonly Regex HtmlTags = new(@"</?([A-Za-z][A-Za-z0-9]*)\b[^>]*>", RegexOptions.Compiled);

    public static string Export(string translationId, string sourceLocale, string targetLocale,
        IEnumerable<TranslationValueDto> values)
    {
        var document = new XDocument(
            new XElement(XliffNamespace + "xliff",
                new XAttribute("version", "2.0"),
                new XAttribute("srcLang", sourceLocale),
                new XAttribute("trgLang", targetLocale),
                new XElement(XliffNamespace + "file",
                    new XAttribute("id", translationId),
                    values.Select(value => new XElement(XliffNamespace + "unit",
                        new XAttribute("id", value.Id),
                        new XElement(XliffNamespace + "segment",
                            new XElement(XliffNamespace + "source", value.SourceValue),
                            new XElement(XliffNamespace + "target",
                                value.Translations.GetValueOrDefault(targetLocale) ?? string.Empty)))))));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    public static Dictionary<string, string> Import(string xliff, string translationId, string sourceLocale,
        string targetLocale,
        IReadOnlyDictionary<string, TranslationValueDto> currentValues)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(xliff, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception)
        {
            throw new PluginMisconfigurationException($"Content file is not valid XML: {exception.Message}");
        }

        var root = document.Root;
        if (root?.Name != XliffNamespace + "xliff" || (string?)root.Attribute("version") != "2.0")
            throw new PluginMisconfigurationException("Content file must be XLIFF 2.0 from Download.");

        var file = root.Elements().SingleOrDefault(element => element.Name.LocalName == "file");
        if (file is null || !string.Equals((string?)file.Attribute("id"), translationId, StringComparison.Ordinal))
            throw new PluginMisconfigurationException("XLIFF file ID does not match Translation ID.");

        if (!string.Equals((string?)root.Attribute("trgLang"), targetLocale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("XLIFF target language does not match Target locale.");
        if (!string.Equals((string?)root.Attribute("srcLang"), sourceLocale, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("XLIFF source language does not match Klaviyo.");

        var transformation = Xliff2Serializer.Deserialize(xliff);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var fileUnits = file.Descendants()
            .Where(element => element.Name.LocalName == "unit")
            .ToDictionary(element => (string?)element.Attribute("id") ?? string.Empty,
                element => element, StringComparer.Ordinal);
        var parsedUnits = transformation.GetUnits().ToArray();
        if (parsedUnits.Length != fileUnits.Count)
            throw new PluginMisconfigurationException("XLIFF contains units that Blackbird.Filters could not parse.");

        foreach (var unit in parsedUnits)
        {
            var valueId = unit.Id;
            if (string.IsNullOrWhiteSpace(valueId) || !currentValues.TryGetValue(valueId, out var current))
                throw new PluginMisconfigurationException($"Unknown Klaviyo value ID in XLIFF: '{valueId}'.");
            if (!fileUnits.TryGetValue(valueId, out var fileUnit))
                throw new PluginMisconfigurationException($"Missing Klaviyo value ID in XLIFF: '{valueId}'.");

            var sourceElement = fileUnit.Descendants().SingleOrDefault(child => child.Name.LocalName == "source");
            var targetElement = fileUnit.Descendants().SingleOrDefault(child => child.Name.LocalName == "target");
            if (sourceElement is null || targetElement is null ||
                sourceElement.Elements().Any() || targetElement.Elements().Any())
                throw new PluginMisconfigurationException(
                    $"XLIFF value '{valueId}' must contain plain-text source and target elements.");

            if (!string.Equals(NormalizeLineEndings(sourceElement.Value),
                    NormalizeLineEndings(current.SourceValue), StringComparison.Ordinal))
                throw new PluginMisconfigurationException(
                    $"Klaviyo source value '{valueId}' changed since Download. Download a fresh file.");
            // Filters validates XLIFF units; XML text retains line breaks that Filters normalizes.
            if (!result.TryAdd(valueId, targetElement.Value))
                throw new PluginMisconfigurationException($"Duplicate Klaviyo value ID in XLIFF: '{valueId}'.");

            var target = result[valueId];
            if (string.IsNullOrWhiteSpace(target))
            {
                result.Remove(valueId);
                continue;
            }

            ValidateHtmlTranslation(current.SourceValue, target, valueId);
        }

        if (result.Count == 0)
            throw new PluginMisconfigurationException("XLIFF does not contain any translated target values.");

        return result;
    }

    public static void ValidateHtmlTranslation(string source, string target, string valueId)
    {
        var sourceTags = HtmlTags.Matches(source)
            .Select(match => (match.Value.StartsWith("</", StringComparison.Ordinal) ? "/" : "")
                             + match.Groups[1].Value.ToLowerInvariant())
            .ToArray();
        if (sourceTags.Length == 0)
            return;

        if (source.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) &&
            !target.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException(
                $"Translated HTML for '{valueId}' must preserve the source doctype.");

        var targetTags = HtmlTags.Matches(target)
            .Select(match => (match.Value.StartsWith("</", StringComparison.Ordinal) ? "/" : "")
                             + match.Groups[1].Value.ToLowerInvariant())
            .ToArray();
        if (!sourceTags.SequenceEqual(targetTags))
            throw new PluginMisconfigurationException(
                $"Translated HTML for '{valueId}' must preserve the source HTML tag structure.");
    }

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
}
