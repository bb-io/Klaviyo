using Apps.Klaviyo.Constants;
using Apps.Klaviyo.Helpers;
using Apps.Klaviyo.Services.Content.Concrete;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Klaviyo.Services.Content;

public class ContentServiceFactory(
    InvocationContext invocationContext,
    IFileManagementClient fileManagementClient)
{
    public IContentService GetContentService(string contentType)
    {
        var normalizedType = contentType.Trim().ToLowerInvariant();
        return normalizedType switch
        {
            TranslationResourceTypes.Template =>
                new TemplateContentService(invocationContext, fileManagementClient),
            TranslationResourceTypes.CampaignVariation =>
                new CampaignVariationContentService(invocationContext, fileManagementClient),
            TranslationResourceTypes.FlowMessage =>
                new FlowMessageContentService(invocationContext, fileManagementClient),
            TranslationResourceTypes.UniversalContent =>
                new UniversalContentService(invocationContext, fileManagementClient),
            _ => throw ExceptionHelper.UnsupportedContentType()
        };
    }
}
