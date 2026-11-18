using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Text;

namespace Apps.Klaviyo.Services;

public static class TranslationFileService
{
    public static async Task<TranslationHtmlFile> ReadAsync(
        IFileManagementClient fileManagementClient,
        FileReference file)
    {
        using var stream = await fileManagementClient.DownloadAsync(file);
        return await TranslationContentReader.ReadAsync(stream, file.Name);
    }

    public static async Task<FileReference> UploadTextAsync(
        IFileManagementClient fileManagementClient,
        string content,
        string mediaType,
        string fileName)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return await fileManagementClient.UploadAsync(stream, mediaType, fileName);
    }
}
