using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Transformations;
using System.Text;

namespace Apps.Klaviyo.Services;

public static class TranslationContentReader
{
    public static async Task<TranslationHtmlFile> ReadAsync(Stream stream, string? fileName)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "content.html" : fileName;
        var transformation = Transformation.Load(stream, name, "text/html");
        if (!transformation.Success)
            throw new PluginMisconfigurationException(
                $"Сould not read the content file: {transformation.Error}");

        if (!transformation.WasBilingual &&
            Path.GetExtension(name).ToLowerInvariant() is not (".html" or ".htm"))
            throw new PluginMisconfigurationException("Upload accepts HTML or XLIFF files.");

        var content = transformation.WasBilingual
            ? transformation.Target()
            : transformation.Source();
        if (!content.Success)
            throw new PluginMisconfigurationException(
                $"Сould not restore the content file: {content.Error}");

        if (!string.Equals(content.Value.OriginalMediaType, "text/html", StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException(
                "Upload requires HTML or an XLIFF file created from the downloaded HTML.");

        using var restoredStream = content.Value.ToStream();
        using var reader = new StreamReader(restoredStream, Encoding.UTF8);
        var html = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(html))
            throw new PluginMisconfigurationException("Content file is empty.");

        return TranslationHtmlFileCodec.Import(html);
    }
}
