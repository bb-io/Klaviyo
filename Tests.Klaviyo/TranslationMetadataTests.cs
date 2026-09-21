using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Services;

namespace Tests.Klaviyo;

[TestClass]
public class TranslationMetadataTests
{
    [DataTestMethod]
    [DataRow(TranslationResourceTypes.Template, "Template")]
    [DataRow(TranslationResourceTypes.CampaignVariation, "Campaign variation")]
    [DataRow(TranslationResourceTypes.FlowMessage, "Flow message")]
    [DataRow(TranslationResourceTypes.UniversalContent, "Universal content")]
    public async Task ExportedMetadataCanBeResolvedForUpload(string resourceType, string resourceDisplayName)
    {
        const string resourceId = "01M2GDN3A24Z8Q6T5QS9R7KBV";
        var translationId = $"{resourceType}::{TranslationChannels.Email}::{resourceId}";
        var html = TranslationHtmlFileCodec.Export(
            new Dictionary<string, string>
            {
                [TranslationMetadataKeys.ContentType] = resourceType,
                [TranslationMetadataKeys.ResourceId] = resourceId,
                [TranslationMetadataKeys.Channel] = TranslationChannels.Email,
                [TranslationMetadataKeys.TranslationId] = translationId
            },
            new Dictionary<string, string> { ["value-id"] = "value" });

        var downloadableHtml = TemplateHtmlFilterService.Create(html, "content.html");
        StringAssert.Contains(downloadableHtml, "name=\"blackbird-content-type\"");
        StringAssert.Contains(downloadableHtml, "name=\"blackbird-resource-id\"");
        StringAssert.Contains(downloadableHtml, "name=\"blackbird-channel\"");
        StringAssert.Contains(downloadableHtml, "name=\"blackbird-translation-id\"");

        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(downloadableHtml));
        var imported = await TranslationContentReader.ReadAsync(stream, "content.html");
        var (resolvedResourceId, resolvedChannel) =
            TranslationChannelHelper.ResolveUploadResourceReference(
                null,
                imported.Metadata,
                resourceType,
                TranslationChannels.ForResourceType(resourceType),
                resourceDisplayName);

        Assert.AreEqual(resourceId, resolvedResourceId);
        Assert.AreEqual(TranslationChannels.Email, resolvedChannel);
    }
}
