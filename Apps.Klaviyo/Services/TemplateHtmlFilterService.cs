using System.Text;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Coders;
using Blackbird.Filters.Transformations;

namespace Apps.Klaviyo.Services;

public static class TemplateHtmlFilterService
{
    public static string Create(string html, string fileName)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
        var transformation = Transformation.Load(stream, fileName, "text/html");
        if (!transformation.Success)
            throw new PluginMisconfigurationException(
                $"Blackbird Filters could not process the template HTML: {transformation.Error}");

        var source = transformation.Value.Source();
        if (!source.Success)
            throw new PluginMisconfigurationException(
                $"Blackbird Filters could not create the template HTML: {source.Error}");

        var result = new HtmlCoder().Serialize(source.Value);
        return !string.IsNullOrWhiteSpace(result)
            ? result
            : throw new PluginApplicationException("Blackbird Filters returned empty template HTML.");
    }
}
