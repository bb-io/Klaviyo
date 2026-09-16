using Blackbird.Applications.Sdk.Common.Exceptions;
using HtmlAgilityPack;

namespace Apps.Klaviyo.Services;

public static class TranslationHtmlFileCodec
{
    public const string TranslationKeyAttribute = "data-klaviyo-translation-key";
    private const string MetadataPrefix = "blackbird-";

    public static string Export(
        IReadOnlyDictionary<string, string> metadata,
        IEnumerable<KeyValuePair<string, string>> values)
    {
        var document = new HtmlDocument();
        var htmlNode = document.CreateElement("html");
        document.DocumentNode.AppendChild(htmlNode);

        var headNode = document.CreateElement("head");
        htmlNode.AppendChild(headNode);

        var charsetNode = document.CreateElement("meta");
        charsetNode.SetAttributeValue("charset", "utf-8");
        headNode.AppendChild(charsetNode);

        foreach (var (key, value) in metadata)
        {
            var metaNode = document.CreateElement("meta");
            metaNode.SetAttributeValue("name", $"{MetadataPrefix}{key}");
            metaNode.SetAttributeValue("content", value);
            headNode.AppendChild(metaNode);
        }

        var bodyNode = document.CreateElement("body");
        htmlNode.AppendChild(bodyNode);

        foreach (var (id, value) in values)
        {
            var valueNode = document.CreateElement("div");
            valueNode.SetAttributeValue(TranslationKeyAttribute, id);
            valueNode.InnerHtml = ToFragment(value);
            bodyNode.AppendChild(valueNode);
        }

        return document.DocumentNode.OuterHtml;
    }

    public static TranslationHtmlFile Import(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var metadata = document.DocumentNode.Descendants("meta")
            .Select(node => new
            {
                Name = node.GetAttributeValue("name", string.Empty),
                Value = node.GetAttributeValue("content", string.Empty)
            })
            .Where(item => item.Name.StartsWith(MetadataPrefix, StringComparison.OrdinalIgnoreCase))
            .GroupBy(item => item.Name[MetadataPrefix.Length..], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);

        var valueNodes = document.DocumentNode.Descendants()
            .Where(node => node.Attributes[TranslationKeyAttribute] is not null)
            .ToArray();
        var duplicateId = valueNodes
            .GroupBy(node => node.GetAttributeValue(TranslationKeyAttribute, string.Empty),
                StringComparer.Ordinal)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);
        if (duplicateId is not null)
            throw new PluginMisconfigurationException(
                string.IsNullOrWhiteSpace(duplicateId.Key)
                    ? $"Every '{TranslationKeyAttribute}' attribute must have a value."
                    : $"Translation value ID '{duplicateId.Key}' occurs more than once in the HTML file.");

        var values = valueNodes.ToDictionary(
            node => node.GetAttributeValue(TranslationKeyAttribute, string.Empty),
            node => node.InnerHtml,
            StringComparer.Ordinal);
        if (values.Count == 0)
            throw new PluginMisconfigurationException(
                $"The HTML file contains no elements with a '{TranslationKeyAttribute}' attribute.");

        return new TranslationHtmlFile(metadata, values);
    }

    public static string ToFragment(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        return document.DocumentNode.SelectSingleNode("//body")?.InnerHtml
               ?? document.DocumentNode.InnerHtml;
    }
}

public record TranslationHtmlFile(
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyDictionary<string, string> Values);
