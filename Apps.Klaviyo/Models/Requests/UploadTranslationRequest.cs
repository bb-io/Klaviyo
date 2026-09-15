using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Klaviyo.Models.Requests;

public class UploadTranslationRequest : IUploadContentInput
{
    [Display("Translation ID", Description = "The composite translation ID. Must match the XLIFF file ID.")]
    public string? ContentId { get; set; }

    [Display("Target locale", Description = "Must match the XLIFF target language.")]
    public string Locale { get; set; } = string.Empty;

    [Display("Content file", Description = "A translated XLIFF 2.0 file from Download.")]
    public FileReference Content { get; set; } = default!;
}
