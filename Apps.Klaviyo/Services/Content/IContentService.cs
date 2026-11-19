using Apps.Klaviyo.Services.Content.Models;

namespace Apps.Klaviyo.Services.Content;

public interface IContentService
{
    Task<ContentDownloadResult> DownloadAsync(ContentDownloadServiceRequest request);

    Task UploadAsync(ContentUploadServiceRequest request);
}
