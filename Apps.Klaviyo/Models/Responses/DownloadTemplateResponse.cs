using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Klaviyo.Models.Responses;

public class DownloadTemplateResponse
{
    [Display("Content (HTML)")]
    public FileReference Content { get; set; } = default!;

    [Display("Template data (JSON)")]
    public FileReference JsonFile { get; set; } = default!;

    [Display("Translation (XLIFF)")]
    public FileReference? XliffFile { get; set; }
}
