using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Apps.Klaviyo.Api.Dtos;
using Blackbird.Applications.Sdk.Common.Exceptions;
using HtmlAgilityPack;

namespace Apps.Klaviyo.Services;

public static class TemplateHtmlCodec
{
    private const string ValueIdAttribute = "data-klaviyo-value-id";
    private const string FormatAttribute = "data-klaviyo-value-format";
    private const string SourceHashAttribute = "data-klaviyo-source-hash";
    private static readonly Regex HtmlTag = new(@"<[/!]?[A-Za-z][^>]*>", RegexOptions.Compiled);

    public static string Export(string templateId, string? locale, IReadOnlyCollection<TranslationValueDto> values)
    {
        var html = new StringBuilder("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
        html.Append("<meta name=\"klaviyo-template-id\" content=\"")
            .Append(WebUtility.HtmlEncode(templateId)).Append("\">");
        html.Append("</head><body>");

        foreach (var value in values)
        {
            var target = value.Translations.FirstOrDefault(item =>
                string.Equals(item.Key, locale, StringComparison.OrdinalIgnoreCase)).Value;
            var content = string.IsNullOrWhiteSpace(target) ? value.SourceValue : target;
            var isHtml = ContainsHtml(value.SourceValue);
            html.Append("<div ").Append(ValueIdAttribute).Append("=\"")
                .Append(WebUtility.HtmlEncode(value.Id)).Append("\" ")
                .Append(FormatAttribute).Append("=\"")
                .Append(isHtml ? "html" : "text").Append("\" ")
                .Append(SourceHashAttribute).Append("=\"")
                .Append(Hash(value.SourceValue)).Append("\">")
                .Append(isHtml ? content : WebUtility.HtmlEncode(content))
                .Append("</div>");
        }

        return html.Append("</body></html>").ToString();
    }

    public static Dictionary<string, string> Import(string html, string templateId,
        IReadOnlyCollection<TranslationValueDto> currentValues)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        var fileTemplateId = document.DocumentNode.SelectSingleNode("//meta[@name='klaviyo-template-id']")
            ?.GetAttributeValue("content", string.Empty);
        if (!string.Equals(fileTemplateId, templateId, StringComparison.Ordinal))
            throw new PluginMisconfigurationException("HTML file does not match Template ID.");

        var nodes = document.DocumentNode.Descendants()
            .Where(node => node.Attributes[ValueIdAttribute] is not null).ToArray();
        var currentById = currentValues.ToDictionary(value => value.Id, StringComparer.Ordinal);
        if (nodes.Length != currentById.Count)
            throw new PluginMisconfigurationException("HTML file must contain every Klaviyo translation value once.");

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            var id = node.GetAttributeValue(ValueIdAttribute, string.Empty);
            if (!currentById.TryGetValue(id, out var current) || result.ContainsKey(id))
                throw new PluginMisconfigurationException($"Unknown or duplicate Klaviyo value ID in HTML: '{id}'.");
            if (!string.Equals(node.GetAttributeValue(SourceHashAttribute, string.Empty),
                    Hash(current.SourceValue), StringComparison.Ordinal))
                throw new PluginMisconfigurationException(
                    $"Klaviyo source value '{id}' changed since Download. Download a fresh file.");

            var format = node.GetAttributeValue(FormatAttribute, string.Empty);
            if (format != (ContainsHtml(current.SourceValue) ? "html" : "text"))
                throw new PluginMisconfigurationException($"Klaviyo value '{id}' has an invalid HTML format.");
            if (format == "text" && node.ChildNodes.Any(child => child.NodeType == HtmlNodeType.Element))
                throw new PluginMisconfigurationException($"Klaviyo value '{id}' must contain plain text.");
            var content = format == "html" ? node.InnerHtml : WebUtility.HtmlDecode(node.InnerHtml);
            if (string.IsNullOrWhiteSpace(content))
                throw new PluginMisconfigurationException($"Klaviyo value '{id}' is empty.");
            TranslationFileCodec.ValidateHtmlTranslation(current.SourceValue, content, id);
            result.Add(id, content);
        }

        return result;
    }

    private static bool ContainsHtml(string value) => HtmlTag.IsMatch(value);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
